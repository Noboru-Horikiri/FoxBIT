extern alias AyonixLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using AyonixLib.Ayonix.FaceID;
using FoxBIT.Ayonix.DB;
using FoxBIT.Ayonix.IPCCommon.Models;
using FoxBIT.IPC;
#if DEBUG
using System.Threading;
using System.Globalization;
#endif

namespace FoxBIT.Ayonix.IPCCommon
{
    /// <summary>
    /// Ayonix顔認識APIクラス
    /// </summary>
    public class AyonixSharedClass : SharedClass
    {
        // AyonixFaceエンジン
        private AyonixFaceID _faceEngine;
        // PinnedAfid
        private PinnedAfidList _pinnedAfids;
        // AFIDリスト
        private List<byte[]> _afidList;
        // AFIDに紐づく顔情報リスト
        private List<AFIDInfomation> _faceInfomationList;
        // 排他用オブジェクト
        private object _objLock;
        // AyonixWebAPIDBクラス
        private AyonixWebAPIDB _ayonixWebAPIDB = new AyonixWebAPIDB();
        // 検出する顔の最小サイズ
        public int MinFaceSize { get; set; } = 80;
        // 検出する顔のクオリティ
        public int MinFaceQuality { get; set; } = 80;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AyonixSharedClass()
        {
#if DEBUG
            // カレント言語を英語とする
            Thread.CurrentThread.CurrentUICulture =
            CultureInfo.GetCultureInfoByIetfLanguageTag("en");
#endif
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Initialize()
        {
            var faceEnginePath = $@"{System.AppDomain.CurrentDomain.BaseDirectory}data\engine";

            try
            {
                // AyonixFaceエンジン初期化
                // Ayonix.FaceID.dllは下記モジュールに依存しているので実行フォルダに配置する必要がある
                //  - AyonixFaceID.dll
                //  - FreeImage.dll
                _faceEngine = new AyonixFaceID(faceEnginePath);
            }
            catch (Exception err)
            {
                System.Diagnostics.Trace.WriteLine($"{err.Message}:Ayonix.FaceID.dllはAyonixFaceID.dll, FreeImage.dllに依存しているので確認");
                throw new Exception($"{err.Message}:Ayonix.FaceID.dllはAyonixFaceID.dll, FreeImage.dllに依存しているので確認");
            }
            _pinnedAfids = null;
            _afidList = new List<byte[]>();
            _faceInfomationList = new List<AFIDInfomation>();
            _objLock = new object();
        }

        /// <summary>
        /// Base64画像文字列から画像変換
        /// </summary>
        /// <param name="base64Image">Base64画像文字列</param>
        /// <returns>変換画像</returns>
        private Bitmap ConvertBase64ImageToBitmap(string base64Image)
        {
            var imageBytes = Convert.FromBase64String(base64Image);
            using (var stream = new MemoryStream())
            {
                stream.Write(imageBytes, 0, imageBytes.Length);
                return new Bitmap(stream);
            }
        }

        /// <summary>
        /// 16進Byte文字列→Byte配列変換
        /// </summary>
        /// <param name="hex">16進Byte文字列</param>
        /// <returns>変換後のByte配列</returns>
        private static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        /// <summary>
        /// 画像から顔リストを作成
        /// </summary>
        /// <param name="bitmap">作成元画像</param>
        /// <returns>結果顔リスト</returns>
        private Face[] CreateFaceListInfomation(Bitmap bitmap)
        {
            // ビットマップからAyonixイメージに変換
            AyonixLib.Ayonix.FaceID.Image img = new AyonixLib.Ayonix.FaceID.Image(bitmap);

            // Ayonixイメージから顔検出
            Face[] faces = new Face[0];
            _faceEngine.DetectFaces(img, MinFaceSize, out faces);

            // 顔の検出チェック
            if (faces.Count() < 1)
            {
                throw new Exception("顔が検出できませんでした。");
            }

            // 顔詳細を取得
            _faceEngine.PreprocessFaces(faces);
            
            // 一番サイズが大きい順で並べ替え
            return faces.OrderByDescending(f => f.location.w + f.location.h).ToArray();
        }

        /// <summary>
        /// 顔情報リストに情報を追加
        /// </summary>
        /// <param name="faceID">登録対象FaceID</param>
        /// <param name="faceSubID">登録対象FaceSubID</param>
        /// <param name="afid">登録対象AFID</param>
        /// <param name="afidString">登録対象AFID文字列</param>
        private void AddPinnedAfid(string faceID, string faceSubID, byte[] afid, string afidString)
        {
            // AFIDに紐づく顔情報を作成
            var afidInfomation = new AFIDInfomation()
            {
                FaceID = faceID,
                FaceSubID = faceSubID,
                AFID = afid,
                AFIDString = afidString
            };

            // リストに登録
            lock (_objLock)
            {
                // AFIDをリストに登録
                _afidList.Add(afidInfomation.AFID);
                // AFIDに紐づく顔情報をリストに登録
                _faceInfomationList.Add(afidInfomation);
                // AFIDリストでPinnedAfidリストを構築
                _faceEngine.PinAfids(_afidList, out _pinnedAfids);
            }
        }

        /// <summary>
        /// 顔情報リストの情報を更新
        /// </summary>
        /// <param name="faceID">更新対象FaceID</param>
        /// <param name="faceSubID">更新対象FaceSubID</param>
        /// <param name="afid">更新対象AFID</param>
        /// <param name="afidString">更新対象AFID文字列</param>
        private void UpdatePinnedAfid(string faceID, string faceSubID, byte[] afid, string afidString)
        {
            // 更新対象のインデックスで絞込む
            var index = _faceInfomationList
                //.Select((fi, i) => new { fi = fi, index = i })
                .Select((fi, i) => new { fi, index = i })
                .Where(fi => fi.fi.FaceID.Equals(faceID) && fi.fi.FaceSubID.Equals(faceSubID))
                .Select(fi => fi.index)
                .FirstOrDefault();

            // AFIDに紐づく顔情報を作成
            var afidInfomation = new AFIDInfomation()
            {
                FaceID = faceID,
                FaceSubID = faceSubID,
                AFID = afid,
                AFIDString = afidString
            };

            lock (_objLock)
            {
                // AFIDをリストから削除
                _afidList.RemoveAt(index);
                // AFIDに紐づく顔情報をリストから削除
                _faceInfomationList.RemoveAt(index);
                // AFIDをリストに登録
                _afidList.Add(afidInfomation.AFID);
                // AFIDに紐づく顔情報をリストに登録
                _faceInfomationList.Add(afidInfomation);
                // AFIDリストでPinnedAfidリストを構築
                _faceEngine.PinAfids(_afidList, out _pinnedAfids);
            }
        }

        /// <summary>
        /// 顔情報リストから情報を削除
        /// </summary>
        /// <param name="faceID">削除対象FaceID</param>
        private void DeletePinnedAfid(string faceID)
        {
            // 削除対象のインデックスで絞込む
            var indexList = _faceInfomationList
                //.Select((fi, i) => new { fi = fi, index = i })
                .Select((fi, i) => new { fi, index = i })
                .Where(fi => fi.fi.FaceID.Equals(faceID))
                .Select(fi => fi.index)
                .ToList();

            // 絞り込み結果チェック
            if (indexList.Count == 0)
            {
                return;
            }

            lock (_objLock)
            {
                indexList.ForEach(index =>
                {
                    // AFIDをリストから削除
                    _afidList.RemoveAt(index);
                    // AFIDに紐づく顔情報をリストから削除
                    _faceInfomationList.RemoveAt(index);
                });
                // AFIDリストでPinnedAfidリストを構築
                _faceEngine.PinAfids(_afidList, out _pinnedAfids);
            }
        }

        /// <summary>
        /// 顔情報リストから情報を削除
        /// </summary>
        /// <param name="faceID">削除対象FaceID</param>
        /// <param name="faceSubID">削除対象FaceSubID</param>
        private void DeletePinnedAfid(string faceID, string faceSubID)
        {
            // 削除対象のインデックスで絞込む
            var index = _faceInfomationList
                //.Select((fi, i) => new { fi = fi, index = i })
                .Select((fi, i) => new { fi, index = i })
                .Where(fi => fi.fi.FaceID.Equals(faceID) && fi.fi.FaceSubID.Equals(faceSubID))
                .Select(fi => fi.index)
                .FirstOrDefault();

            lock (_objLock)
            {
                // AFIDをリストから削除
                _afidList.RemoveAt(index);
                // AFIDに紐づく顔情報をリストから削除
                _faceInfomationList.RemoveAt(index);
                // AFIDリストでPinnedAfidリストを構築
                _faceEngine.PinAfids(_afidList, out _pinnedAfids);
            }
        }

        /// <summary>
        /// AFIDリスト数取得
        /// </summary>
        /// <returns>AFIDリスト数</returns>
        public int CountAFID()
        {
            return _afidList.Count;
        }

        /// <summary>
        /// Base64画像文字列から顔を検出し数取得
        /// </summary>
        /// <param name="base64Image">Base64画像文字列</param>
        /// <returns>検出顔数</returns>
        public int CountFace(string base64Image)
        {
            // 画像から顔情報取得
            Face[] faceList;
            using (var bitmap = ConvertBase64ImageToBitmap(base64Image))
            {
                faceList = CreateFaceListInfomation(bitmap);
            }

            return faceList.Count();
        }

        /// <summary>
        /// AFIDをDBから読込み
        /// </summary>
        public void LoadFaces()
        {
            // AFIDをDBから取得
            var enableFaceSubID = _ayonixWebAPIDB.GetEnableFaceID();

            enableFaceSubID.ForEach(efsi =>
            {
                // AFIDリストに追加
                AddPinnedAfid(efsi.id, efsi.sub_id, StringToByteArray(efsi.afid), efsi.afid);
            });

        }

        /// <summary>
        /// Base64画像文字列から顔情報新規登録
        /// </summary>
        /// <param name="base64Image">Base64画像文字列から</param>
        /// <returns>登録結果</returns>
        public ResultDetectedFace CreateFaceID(string base64Image)
        {
            // 画像から顔情報取得
            Face[] faceList;
            using (var bitmap = ConvertBase64ImageToBitmap(base64Image))
            {
                faceList = CreateFaceListInfomation(bitmap);
            }

            // 一番サイズが大きい顔情報を取得
            var face = faceList.OrderByDescending(f => f.location.w + f.location.h).First();

            // AFIDを作成
            _faceEngine.CreateAfid(ref face, out byte[] afid);
            // AFID作成失敗
            if (afid == null)
            {
                throw new Exception("AFIDの作成に失敗しました。");
            }

            // AFIDの16進文字列化
            var afidString = BitConverter.ToString(afid).Replace("-", string.Empty);

            // AFIDをDBに登録
            var resultFaceID = _ayonixWebAPIDB.AddFaceID(afidString);

            // AFIDリストに追加
            AddPinnedAfid(resultFaceID.id, resultFaceID.face_sub_ids.First().sub_id, afid, afidString);

            // 結果作成
            return new ResultDetectedFace()
            {
                FaceID = resultFaceID.id,
                FaceSubID = resultFaceID.face_sub_ids.First().sub_id,
                AFID = resultFaceID.face_sub_ids.First().afid,
                MugLocation = new Location()
                {
                    //X = face.mugLocation.x,
                    //Y = face.mugLocation.y,
                    //W = face.mugLocation.w,
                    //H = face.mugLocation.h
                    X = face.location.x,
                    Y = face.location.y,
                    W = face.location.w,
                    H = face.location.h
                }
            };
        }

        /// <summary>
        /// 顔情報一括削除
        /// </summary>
        /// <param name="faceID">削除対象FaceID</param>
        public void DeleteFaceID(string faceID)
        {
            // DB上の対象FaceIDの削除フラグをONにする
            if (!_ayonixWebAPIDB.DeleteFaceID(faceID))
            {
                throw new Exception("AFIDのDB削除フラグの更新に失敗しました。");
            }

            // AFIDリストから削除
            DeletePinnedAfid(faceID);
        }

        /// <summary>
        /// Base64画像文字列から顔情報追加登録
        /// </summary>
        /// <param name="faceID">追加対象FaceID</param>
        /// <param name="base64Image">Base64画像文字列</param>
        /// <returns>追加結果</returns>
        public ResultDetectedFace AddFace(string faceID, string base64Image)
        {
            // 画像から顔情報取得
            Face[] faceList;
            using (var bitmap = ConvertBase64ImageToBitmap(base64Image))
            {
                faceList = CreateFaceListInfomation(bitmap);
            }

            // 一番サイズが大きい顔情報を取得
            var face = faceList.OrderByDescending(f => f.location.w + f.location.h).First();

            // AFIDを作成
            _faceEngine.CreateAfid(ref face, out byte[] afid);
            // AFID作成失敗
            if (afid == null)
            {
                throw new Exception("AFIDの作成に失敗しました。");
            }

            // AFIDの16進文字列化
            var afidString = BitConverter.ToString(afid).Replace("-", string.Empty);

            // AFIDをDBに登録
            var resultFaceID = _ayonixWebAPIDB.AddFaceSubID(faceID, afidString);
            if (resultFaceID == null)
            {
                throw new Exception("AFIDのDB登録に失敗しました。");
            }

            // AFIDリストに追加
            AddPinnedAfid(resultFaceID.id, resultFaceID.face_sub_ids.First().sub_id, afid, afidString);

            // 結果作成
            return new ResultDetectedFace()
            {
                FaceID = resultFaceID.id,
                FaceSubID = resultFaceID.face_sub_ids.First().sub_id,
                AFID = resultFaceID.face_sub_ids.First().afid,
                MugLocation = new Location()
                {
                    //X = face.mugLocation.x,
                    //Y = face.mugLocation.y,
                    //W = face.mugLocation.w,
                    //H = face.mugLocation.h
                    X = face.location.x,
                    Y = face.location.y,
                    W = face.location.w,
                    H = face.location.h
                }
            };
        }

        /// <summary>
        /// Base64画像文字列から顔情報更新
        /// </summary>
        /// <param name="faceID">更新対象FaceID</param>
        /// <param name="faceSubID">更新対象FaceSubID</param>
        /// <param name="base64Image">Base64画像文字列</param>
        public void UpdateFace(string faceID, string faceSubID, string base64Image)
        {
            // 画像から顔情報取得
            Face[] faceList;
            using (var bitmap = ConvertBase64ImageToBitmap(base64Image))
            {
                faceList = CreateFaceListInfomation(bitmap);
            }

            // 一番サイズが大きい顔情報を取得
            var face = faceList.OrderByDescending(f => f.location.w + f.location.h).First();

            // AFIDを作成
            _faceEngine.CreateAfid(ref face, out byte[] afid);
            // AFID作成失敗
            if (afid == null)
            {
                throw new Exception("AFIDの作成に失敗しました。");
            }

            // AFIDの16進文字列化
            var afidString = BitConverter.ToString(afid).Replace("-", string.Empty);

            // DB上の対象FaceID, FaceSubIDのAFIDを更新する
            if (!_ayonixWebAPIDB.UpdateFaceSubID(faceID, faceSubID, afidString))
            {
                throw new Exception("AFIDの更新に失敗しました。");
            }

            // AFIDリストを更新
            UpdatePinnedAfid(faceID, faceSubID, afid, afidString);
        }

        /// <summary>
        /// 顔情報個別削除
        /// </summary>
        /// <param name="faceID">削除対象FaceID</param>
        /// <param name="faceSubID">削除対象FaceSubID</param>
        public void DeleteFaceID(string faceID, string faceSubID)
        {
            // DB上の対象FaceID, FaceSubIDのAFIDの削除フラグをONにする
            if (!_ayonixWebAPIDB.DeleteFaceSubID(faceID, faceSubID))
            {
                throw new Exception("AFIDのDB削除フラグの更新に失敗しました。");
            }

            // AFIDリストから削除
            DeletePinnedAfid(faceID, faceSubID);
        }

        /// <summary>
        /// Base64画像文字列に存在する顔情報比較
        /// </summary>
        /// <param name="base64Image">Base64画像文字列</param>
        /// <returns>比較結果</returns>
        public List<ResultComparedFace> CompareFace(string base64Image)
        {
            if (_pinnedAfids == null)
            {
                throw new Exception("AFIDが登録されていません。");
            }

            Face[] faceList;
            using (var bitmap = ConvertBase64ImageToBitmap(base64Image))
            {
                faceList = CreateFaceListInfomation(bitmap);
            }
            
            // 登録済みリストから、検出したすべての顔を比較
            var resultComparedFaces = new List<ResultComparedFace>();
            for (int i = 0; faceList.Count() > i; i++)
            {
                // 顔の妥当性チェック
                if (!faceList[i].isValid) { continue; }
                if (MinFaceQuality > faceList[i].quality * 100) { continue; }

                // AFIDを作成
                _faceEngine.CreateAfid(ref faceList[i], out byte[] afid);
                // AFID作成失敗
                if (afid == null)
                {
                    throw new Exception("AFIDの作成に失敗しました。");
                }

                // 比較処理
                float[] scores;
                int[] indexes;
                lock (_objLock)
                {
                    _faceEngine.MatchAfids(afid, _pinnedAfids, out scores, out indexes);
                }

                // 比較結果を設定
                resultComparedFaces.Add(new ResultComparedFace()
                {
                    No = i + 1,
                    //Score = Math.Truncate(scores[indexes[0]] * 100),
                    Score = scores[indexes[0]] * 100,
                    FaceID = _faceInfomationList[indexes[0]].FaceID,
                    FaceSubID = _faceInfomationList[indexes[0]].FaceSubID,
                    AFID = _faceInfomationList[indexes[0]].AFIDString,
                    MugLocation = new Location()
                    {
                        //X = faceList[i].mugLocation.x,
                        //Y = faceList[i].mugLocation.y,
                        //W = faceList[i].mugLocation.w,
                        //H = faceList[i].mugLocation.h
                        X = faceList[i].location.x,
                        Y = faceList[i].location.y,
                        W = faceList[i].location.w,
                        H = faceList[i].location.h
                    }
                });
            }

            return resultComparedFaces;
        }

        /// <summary>
        /// 顔情報取得
        /// </summary>
        /// <param name="faceID">取得対象FaceID</param>
        /// <returns>取得結果</returns>
        public ResultFaceID GetFaceID(string faceID)
        {
            var enableFaceSubID = _ayonixWebAPIDB.GetEnableFaceID(faceID);

            if (enableFaceSubID.Count() == 0)
            {
                return null;
            }

            var resultFaceID = new ResultFaceID()
            {
                FaceID = enableFaceSubID.First().id,
                FaceSubID = enableFaceSubID.Select(fsi => fsi.sub_id).ToList()
            };

            return resultFaceID;
        }
    }
}
