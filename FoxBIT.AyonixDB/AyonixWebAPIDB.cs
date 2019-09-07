using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Linq;
using FoxBIT.Ayonix.DB.Models;

namespace FoxBIT.Ayonix.DB
{
    /// <summary>
    /// AyonixWebAPIDBクラス
    /// </summary>
    public class AyonixWebAPIDB
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AyonixWebAPIDB()
        {
            // 下記コードがないとビルド時にEntityFrameworkに必要なDLLがコピーされない
            var instance = SqlProviderServices.Instance;
        }

        /// <summary>
        /// 有効な顔情報一覧取得
        /// </summary>
        /// <returns>取得した顔情報一覧</returns>
        public List<face_sub_ids> GetEnableFaceID()
        {
            List<face_sub_ids> resultFaceSubID = null;

            using (var entity = new AyonixWebAPIEntities())
            {
                try
                {
                    // 取得
                    resultFaceSubID = entity.face_ids
                        .Where(fi => fi.deleted == false)
                        .SelectMany(fi => fi.face_sub_ids)
                        .Where(fsi => fsi.deleted == false)
                        .ToList();
                }
                catch (Exception err)
                {
                    System.Diagnostics.Trace.WriteLine(err.Message);
                }
            }

            return resultFaceSubID;
        }

        /// <summary>
        /// 指定したFaceIDの有効な顔情報一覧取得
        /// </summary>
        /// <param name="faceID">指定するFaceID</param>
        /// <returns>取得した顔情報一覧</returns>
        public List<face_sub_ids> GetEnableFaceID(string faceID)
        {
            List<face_sub_ids> resultFaceSubID = null;

            // 取得
            using (var entity = new AyonixWebAPIEntities())
            {
                resultFaceSubID = entity.face_ids
                    .Where(fi => faceID.Equals(fi.id) && fi.deleted == false)
                    //.First()
                    .FirstOrDefault()?
                    .face_sub_ids
                    .Where(fsi => fsi.deleted == false)
                    .ToList();
            }

            return resultFaceSubID ?? new List<face_sub_ids>();
        }

        /// <summary>
        /// 顔情報登録
        /// </summary>
        /// <param name="afid">登録対象AFID</param>
        /// <returns>登録結果の顔情報</returns>
        public face_ids AddFaceID(string afid)
        {
            face_ids result = null;
            try
            {
                using (var entity = new AyonixWebAPIEntities())
                {
                    // 登録するFaceIDを決定（DB最大値 +　1）
                    var maxID = entity.face_ids.Select(fi => fi.id).Max();
                    var newID = Convert.ToInt32(maxID) + 1;

                    // 登録
                    result = entity.face_ids.Add(
                        new face_ids
                        {
                            id = string.Format("{0:00000000}", newID),
                            created_at = DateTime.Now,
                            face_sub_ids = new List<face_sub_ids>()
                            {
                                new face_sub_ids()
                                {
                                    id = string.Format("{0:00000000}", newID),
                                    sub_id = "01",
                                    afid = afid,
                                    created_at = DateTime.Now
                                }
                            }
                        });

                    // コミット
                    entity.SaveChanges();
                }
            }
            catch (Exception err)
            {
                System.Diagnostics.Trace.WriteLine(err.Message);
            }

            return result;
        }

        /// <summary>
        /// 顔情報削除（削除フラグON）
        /// </summary>
        /// <param name="faceID">削除対象FaceID</param>
        /// <returns>削除の成否 true:成功 false:失敗</returns>
        public bool DeleteFaceID(string faceID)
        {
            using (var entity = new AyonixWebAPIEntities())
            {
                // 削除対象抽出
                var target = entity
                    .face_ids
                    .Where(fi => faceID.Equals(fi.id))
                    //.First();
                    .FirstOrDefault();
                // 抽出の成否チェック
                if (target == null)
                {
                    return false;
                }

                // 削除フラグON
                target.deleted = true;
                // 削除時間設定
                target.deleted_at = DateTime.Now;

                // コミット
                entity.SaveChanges();
            }

            return true;
        }

        /// <summary>
        /// 顔情報追加登録
        /// </summary>
        /// <param name="faceID">対象FaceID</param>
        /// <param name="afid">追加登録対象AFID</param>
        /// <returns>登録結果の顔情報</returns>
        public face_ids AddFaceSubID(string faceID, string afid)
        {
            face_ids result = new face_ids() { id = faceID };

            using (var entity = new AyonixWebAPIEntities())
            {
                // 現在のFaceSubIDを取得
                var maxSubID =
                    entity
                    .face_sub_ids
                    .Where(fsi => faceID.Equals(fsi.id))
                    .Select(fsi => fsi.sub_id)
                    .Max();
                // 取得の成否チェック
                if (maxSubID == null)
                {
                    return null;
                }

                // 追加登録するFaceSubIDを決定（DB最大値 +　1）
                var newSubID = Convert.ToInt32(maxSubID) + 1;

                // 登録
                result.face_sub_ids = new List<face_sub_ids>()
                {
                    entity.face_sub_ids.Add(
                        new face_sub_ids
                        {
                            id = faceID,
                            sub_id = string.Format("{0:00}", newSubID),
                            afid = afid,
                            created_at = DateTime.Now
                        })
                };

                // コミット
                entity.SaveChanges();
            }

            return result;
        }

        /// <summary>
        /// 顔情報更新
        /// </summary>
        /// <param name="faceID">更新対象FaceID</param>
        /// <param name="faceSubID">更新対象FaceSubID</param>
        /// <param name="afid">更新AFID</param>
        /// <returns>更新の成否 true:成功 false:失敗</returns>
        public bool UpdateFaceSubID(string faceID, string faceSubID, string afid)
        {
            using (var entity = new AyonixWebAPIEntities())
            {
                // 更新対象抽出
                var target = entity
                    .face_sub_ids
                    .Where(fsi => faceID.Equals(fsi.id) && faceSubID.Equals(fsi.sub_id))
                    //.First();
                    .FirstOrDefault();
                // 抽出の成否チェック
                if (target == null)
                {
                    return false;
                }

                // AFID更新
                target.afid = afid;
                // 更新時間設定
                target.updated_at = DateTime.Now;

                // コミット
                entity.SaveChanges();
            }

            return true;
        }

        /// <summary>
        /// 顔情報削除（FaceSubID毎削除フラグON）
        /// </summary>
        /// <param name="faceID">削除対象FaceID</param>
        /// <param name="faceSubID">削除対象FaceSubID</param>
        /// <returns>削除の成否 true:成功 false:失敗</returns>
        public bool DeleteFaceSubID(string faceID, string faceSubID)
        {
            using (var entity = new AyonixWebAPIEntities())
            {
                // 削除対象抽出
                var target = entity
                    .face_sub_ids
                    .Where(fsi => faceID.Equals(fsi.id) && faceSubID.Equals(fsi.sub_id))
                    //.First();
                    .FirstOrDefault();
                // 抽出の成否チェック
                if (target == null)
                {
                    return false;
                }

                // 削除フラグON
                target.deleted = true;
                // 削除時間設定
                target.deleted_at = DateTime.Now;

                // コミット
                entity.SaveChanges();
            }

            return true;
        }
    }
}
