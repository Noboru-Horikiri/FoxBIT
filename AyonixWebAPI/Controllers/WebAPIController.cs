using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web;
using FoxBIT.Ayonix.IPCCommon.Models;
using FoxBIT.Ayonix.Models;

namespace FoxBIT.Ayonix.Controllers
{
    /// <summary>
    /// AyonixWebAPI Restコントローラークラス
    /// </summary>
    [RoutePrefix("webapi")]
    public class WebAPIController : ApiController
    {
#if DEBUG
        private static Bitmap DrawRect(Bitmap srcBmp, ResultDetectedFace resultDetectedFace)
        {
            using (var graphic = Graphics.FromImage(srcBmp))
            using (var pen = new Pen(Color.FromArgb(128, Color.Blue), 10))
            {
                graphic.DrawRectangle(pen, new Rectangle(resultDetectedFace.MugLocation.X,
                                                         resultDetectedFace.MugLocation.Y,
                                                         resultDetectedFace.MugLocation.W,
                                                         resultDetectedFace.MugLocation.H));

            }
            return srcBmp;
        }
#endif

        /// <summary>
        /// FaceID作成
        /// </summary>
        /// <returns>IHttpActionResult</returns>
        [HttpPost]
        [Route("createfaceid")]
        public async Task<IHttpActionResult> CreateFaceID()
        {
            // コンテンツが "multipart/form-data" かチェック
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            // コンテンツ取得
            var contents = (await 
                Request
                .Content
                .ReadAsMultipartAsync<MultipartMemoryStreamProvider>(new MultipartMemoryStreamProvider())).Contents;

            // 画像枚数チェック
            if (contents.Count == 0)
            {
                return InternalServerError(new Exception("画像ファイルがアップロードされていません。"));
            }
            if (contents.Count > 1)
            {
                return InternalServerError(new Exception("複数の画像ファイルがアップロードされました。処理対象の画像ファイルは1つのみです。"));
            }

            // 画像処理
            ResultDetectedFace resultDetectedFace;
            try
            {
                // アップロードされたファイルをストリームとして取得
                using (var stream = contents[0].ReadAsStreamAsync().Result)
                using (var memStream = new MemoryStream())
                {
                    // メモリーストリームにコピーしてBase64イメージに変換
                    stream.CopyTo(memStream);
                    var imageBytes = memStream.ToArray();
                    var base64Image = Convert.ToBase64String(imageBytes);

                    // 検出顔数チェック
                    var countFace = WebApiApplication.IPCClient.SharedObj.CountFace(base64Image);
                    if (countFace == 0)
                    {
                        return InternalServerError(new Exception("画像ファイルから顔を検出できません。"));
                    }
                    if (countFace > 1)
                    {
                        return InternalServerError(new Exception("画像ファイルから複数の顔を検出しました。"));
                    }

                    // 顔情報新規登録
                    resultDetectedFace = WebApiApplication.IPCClient.SharedObj.CreateFaceID(base64Image);

#if DEBUG
                    // 確認顔保存
                    DrawRect(new Bitmap(memStream), resultDetectedFace).Save(Path.Combine(HttpContext.Current.Server.MapPath("~/"),
                        $@"App_Data/{Path.GetFileNameWithoutExtension(contents[0].Headers.ContentDisposition.FileName)}_{DateTime.Now.ToString("yyyyMMddhhmmssfff")}{Path.GetExtension(contents[0].Headers.ContentDisposition.FileName)}"));
#endif
                }
            }
            catch (Exception err)
            {
                return InternalServerError(new Exception(err.Message));
            }
            
            // レスポンス作成
            return Ok(
                new DetectedFace()
                {
                    ID = resultDetectedFace.FaceID,
                    SubID = resultDetectedFace.FaceSubID,
                    MugLocation = new Models.Location()
                    {
                        x = resultDetectedFace.MugLocation.X,
                        y = resultDetectedFace.MugLocation.Y,
                        w = resultDetectedFace.MugLocation.W,
                        h = resultDetectedFace.MugLocation.H
                    }
                });
        }

        /// <summary>
        /// FaceID削除
        /// </summary>
        /// <param name="faceID">削除対象FaceID</param>
        /// <returns>IHttpActionResult</returns>
        [HttpDelete]
        [Route("deletefaceid/{faceID}")]
        public IHttpActionResult DeleteFaceID(string faceID)
        {
            // IDチェック
            if (faceID.Length != 8)
            {
                return InternalServerError(new Exception("不正なIDです。"));
            }

            try
            {
                // 顔情報一括削除
                WebApiApplication.IPCClient.SharedObj.DeleteFaceID(faceID);
            }
            catch (Exception err)
            {
                return InternalServerError(new Exception(err.Message));
            }

            return Ok();
        }

        /// <summary>
        /// Face追加
        /// </summary>
        /// <param name="faceID">追加対象FaceID</param>
        /// <returns>IHttpActionResult</returns>
        [HttpPost]
        [Route("addface/{faceID}")]
        public async Task<IHttpActionResult> AddFace(string faceID)
        {
            // IDチェック
            if (faceID.Length != 8)
            {
                return InternalServerError(new Exception("不正なIDです。"));
            }

            // コンテンツが "multipart/form-data" かチェック
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            // コンテンツ取得
            var contents = (await
                Request
                .Content
                .ReadAsMultipartAsync<MultipartMemoryStreamProvider>(new MultipartMemoryStreamProvider())).Contents;

            // 画像枚数チェック
            if (contents.Count == 0)
            {
                return InternalServerError(new Exception("画像ファイルがアップロードされていません。"));
            }

            // 画像処理
            var detectedFaces = new List<DetectedFace>();
            foreach (HttpContent content in contents)
            {
                // アップロードされたファイルをストリームとして取得
                using (var stream = content.ReadAsStreamAsync().Result)
                using (var memStream = new MemoryStream())
                {
                    // メモリーストリームにコピーしてBase64イメージに変換
                    stream.CopyTo(memStream);
                    var imageBytes = memStream.ToArray();
                    var base64Image = Convert.ToBase64String(imageBytes);

                    // 検出顔数チェック
                    var countFace = WebApiApplication.IPCClient.SharedObj.CountFace(base64Image);
                    if (countFace == 0)
                    {
                        return InternalServerError(new Exception("画像ファイルから顔を検出できません。"));
                    }
                    if (countFace > 1)
                    {
                        return InternalServerError(new Exception("画像ファイルから複数の顔を検出しました。"));
                    }

                    // 顔情報追加登録
                    var resultDetectedFace = WebApiApplication.IPCClient.SharedObj.AddFace(faceID, base64Image);

                    // レスポンス作成
                    detectedFaces.Add(
                        new DetectedFace()
                        {
                            ID = resultDetectedFace.FaceID,
                            SubID = resultDetectedFace.FaceSubID,
                            MugLocation = new Models.Location()
                            {
                                x = resultDetectedFace.MugLocation.X,
                                y = resultDetectedFace.MugLocation.Y,
                                w = resultDetectedFace.MugLocation.W,
                                h = resultDetectedFace.MugLocation.H
                            }
                        });
                }
            }

            return Ok(detectedFaces);
        }

        /// <summary>
        /// Face更新
        /// </summary>
        /// <param name="faceID">更新対象FaceID</param>
        /// <param name="subID">更新対象FaceSubID</param>
        /// <returns>IHttpActionResult</returns>
        [HttpPut]
        [Route("updateface/{faceID}/{subID}")]
        public async Task<IHttpActionResult> UpdateFace(string faceID, string subID)
        {
            // コンテンツが "multipart/form-data" かチェック
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            // コンテンツ取得
            var contents = (await
                Request
                .Content
                .ReadAsMultipartAsync<MultipartMemoryStreamProvider>(new MultipartMemoryStreamProvider())).Contents;

            // 画像枚数チェック
            if (contents.Count == 0)
            {
                return InternalServerError(new Exception("画像ファイルがアップロードされていません。"));
            }
            if (contents.Count > 1)
            {
                return InternalServerError(new Exception("複数の画像ファイルがアップロードされました。処理対象の画像ファイルは1つのみです。"));
            }
            
            // 画像処理
            try
            {
                // アップロードされたファイルをストリームとして取得
                using (var stream = contents[0].ReadAsStreamAsync().Result)
                using (var memStream = new MemoryStream())
                {
                    // メモリーストリームにコピーしてBase64イメージに変換
                    stream.CopyTo(memStream);
                    var imageBytes = memStream.ToArray();
                    var base64Image = Convert.ToBase64String(imageBytes);

                    // 検出顔数チェック
                    var countFace = WebApiApplication.IPCClient.SharedObj.CountFace(base64Image);
                    if (countFace == 0)
                    {
                        return InternalServerError(new Exception("画像ファイルから顔を検出できません。"));
                    }
                    if (countFace > 1)
                    {
                        return InternalServerError(new Exception("画像ファイルから複数の顔を検出しました。"));
                    }

                    // 顔情報更新
                    WebApiApplication.IPCClient.SharedObj.UpdateFace(faceID, subID, base64Image);
                }
            }
            catch (Exception err)
            {
                return InternalServerError(new Exception(err.Message));
            }

            return Ok();
        }

        /// <summary>
        /// Face削除
        /// </summary>
        /// <param name="faceID">削除対象FaceID</param>
        /// <param name="subID">削除対象FaceSubID</param>
        /// <returns>IHttpActionResult</returns>
        [HttpDelete]
        [Route("deleteface/{faceID}/{subID}")]
        public IHttpActionResult DeleteFace(string faceID, string subID)
        {
            // IDチェック
            if (faceID.Length != 8)
            {
                return InternalServerError(new Exception("不正なIDです。"));
            }

            try
            {
                // 顔情報個別削除
                WebApiApplication.IPCClient.SharedObj.DeleteFaceID(faceID, subID);
            }
            catch (Exception err)
            {
                return InternalServerError(new Exception(err.Message));
            }

            return Ok();
        }

        /// <summary>
        /// 登録済み顔情報取得
        /// </summary>
        /// <param name="faceID">取得対象FaceID</param>
        /// <returns>IHttpActionResult</returns>
        [HttpGet]
        [Route("getfaceid/{faceID}")]
        public IHttpActionResult GetFaceID(string faceID)
        {
            // IDチェック
            if (faceID.Length != 8)
            {
                return InternalServerError(new Exception("不正なIDです。"));
            }

            ResultFaceID resultFaceID;
            try
            {
                // 顔情報取得
                resultFaceID = WebApiApplication.IPCClient.SharedObj.GetFaceID(faceID);
                if (resultFaceID == null)
                {
                    return InternalServerError(new Exception("指定IDに該当する情報が取得できませんでした。"));
                }
            }
            catch (Exception err)
            {
                return InternalServerError(new Exception(err.Message));
            }
            
            return Ok(
                new EnrolledFace()
                {
                    ID = resultFaceID.FaceID,
                    SubID = resultFaceID.FaceSubID
                });
        }

        /// <summary>
        /// 顔比較処理
        /// </summary>
        /// <returns>IHttpActionResult</returns>
        [HttpPost]
        [Route("compareface")]
        public async Task<IHttpActionResult> CompareFace()
        {
            // コンテンツが "multipart/form-data" かチェック
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            // コンテンツ取得
            var contents = (await
                Request
                .Content
                .ReadAsMultipartAsync<MultipartMemoryStreamProvider>(new MultipartMemoryStreamProvider())).Contents;

            // 画像枚数チェック
            if (contents.Count == 0)
            {
                return InternalServerError(new Exception("画像ファイルがアップロードされていません。"));
            }
            if (contents.Count > 1)
            {
                return InternalServerError(new Exception("複数の画像ファイルがアップロードされました。処理対象の画像ファイルは1つのみです。"));
            }

            // 画像処理
            List<ResultComparedFace> resultComparedFaces;
            try
            {
                // アップロードされたファイルをストリームとして取得
                using (var stream = contents[0].ReadAsStreamAsync().Result)
                using (var memStream = new MemoryStream())
                {
                    // メモリーストリームにコピーしてBase64イメージに変換
                    stream.CopyTo(memStream);
                    var imageBytes = memStream.ToArray();
                    var base64Image = Convert.ToBase64String(imageBytes);

                    // 検出顔数チェック
                    var countFace = WebApiApplication.IPCClient.SharedObj.CountFace(base64Image);
                    if (countFace == 0)
                    {
                        return InternalServerError(new Exception("画像ファイルから顔を検出できません。"));
                    }
                    if (countFace > 1)
                    {
                        return InternalServerError(new Exception("画像ファイルから複数の顔を検出しました。"));
                    }

                    // 顔情報比較
                    resultComparedFaces = WebApiApplication.IPCClient.SharedObj.CompareFace(base64Image);
                }
            }
            catch (Exception err)
            {
                return InternalServerError(new Exception(err.Message));
            }

            // レスポンス作成
            var comparedFaces = new List<ComparedFace>();
            resultComparedFaces
                .ForEach(res =>
                {
                    comparedFaces.Add(
                        new ComparedFace()
                        {
                            No = res.No,
                            Score = double.Parse(res.Score.ToString("F2")),
                            ID = res.FaceID,
                            SubID = res.FaceSubID,
                            MugLocation = new FoxBIT.Ayonix.Models.Location()
                            {
                                x = res.MugLocation.X,
                                y = res.MugLocation.Y,
                                w = res.MugLocation.W,
                                h = res.MugLocation.H
                            }
                        });
                });

            return Ok(comparedFaces);
        }

        /// <summary>
        /// 検出する顔の最小サイズ設定
        /// </summary>
        /// <param name="size">最小サイズ</param>
        /// <returns>IHttpActionResult</returns>
        [HttpPut]
        [Route("facesize/min/{size}")]
        public IHttpActionResult SetMinimumFaceSize(int size)
        {
            WebApiApplication.IPCClient.SharedObj.MinFaceSize = size;

            return Ok();
        }

        /// <summary>
        /// 検出する顔のクオリティ設定
        /// </summary>
        /// <param name="quality">クオリティ</param>
        /// <returns></returns>
        [HttpPut]
        [Route("quality/min/{quality}")]
        public IHttpActionResult SetMinimumFacequality(int quality)
        {
            WebApiApplication.IPCClient.SharedObj.MinFaceQuality = quality;

            return Ok();
        }
    }
}
