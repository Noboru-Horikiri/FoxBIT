using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxBIT.Ayonix.DB;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Drawing;
using System.IO;

using System.Diagnostics;

namespace Test
{
    class Program
    {
        private static string BaseAddress { get; set; } = "http://localhost/";
        private static string RootAddress { get; set; } = "Ayonix/webapi";


        private static TraceSource logTraceSource = new TraceSource("LogTraceSource");

        static void Main(string[] args)
        {
            //TestWebAPI();
            //TestTrace();
            CheckResult(GetFaceID("00000003"));
        }

        static void TestTrace()
        {
            logTraceSource.TraceEvent(TraceEventType.Information, 3, "インフォ3");
            logTraceSource.TraceEvent(TraceEventType.Information, 4, "インフォ4");
            
        }

        static void TestWebAPI()
        {
            for (int i = 0; 100 > i; i++)
            {
                // createfaceidのサンプル
                // 単一ファイルのアップロードは成功
                CheckResult(CreateFaceID(new List<string>() { "test2.jpg" }));
                // 複数ファイルのアップロードはエラー
                //CheckResult(CreateFaceID(new List<string>() { "test1.jpg", "test2.jpg" }));
            }

            // deletefaceidのサンプル
            // 正しい桁数のIDは成功（8桁）
            CheckResult(DeleteFaceID("00100001"));
            // 不正な桁数のIDは失敗（8桁以外）
            //CheckResult(DeleteFaceID("0000001"));

            // addfaceのサンプル
            // 正しい桁数のIDは成功（8桁）
            CheckResult(AddFace("00000001", new List<string>() { "test2.jpg", "test1.jpg" }));
            // 不正な桁数のIDは失敗（8桁以外）
            //CheckResult(AddFace("0000001", new List<string>() { "test1.jpg" }));

            // updatefaceのサンプル
            // 正しい桁数のIDは成功（8桁）
            CheckResult(UpdateFace("00000001", "01", new List<string>() { "test1.jpg" }));
            //CheckResult(UpdateFace("00000001", "01", new List<string>() { "test2.jpg" }));
            // 不正な桁数のIDは失敗（8桁以外）
            //CheckResult(UpdateFace("0000001", "01", new List<string>() { "test1.jpg" }));

            // deletefaceのサンプル
            // 正しい桁数のIDは成功（8桁）
            CheckResult(DeleteFace("00000003", "01"));
            // 不正な桁数のIDは失敗（8桁以外）
            //CheckResult(DeleteFace("0000001", "01"));

            // getfaceidのサンプル
            // 正しい桁数のIDは成功（8桁）
            CheckResult(GetFaceID("00000003"));
            // 不正な桁数のIDは失敗（8桁以外）
            //CheckResult(GetFaceID("0000001"));

            // comparefaceのサンプル
            // 単一ファイルのアップロードは成功
            CheckResult(CompareFace(new List<string>() { "test2.jpg" }));
            // アップロードファイルなしはエラー
            //CheckResult(CreateFaceID(new List<string>()));

            Console.ReadLine();
        }

        static void CheckResult(HttpResponseMessage response)
        {
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
            Console.WriteLine(response.StatusCode.ToString());
        }
        
        static private HttpResponseMessage CreateFaceID(List<string> images)
        {
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                client.BaseAddress = new Uri(BaseAddress);

                // ファイル添付
                images.ForEach(img => {
                    img.ToString();
                    var fileContent = new ByteArrayContent(File.ReadAllBytes($@"{AppDomain.CurrentDomain.BaseDirectory}\Images\{img}"));
                    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = img
                    };
                    content.Add(fileContent);
                });

                // 実行
                return client.PostAsync($@"/{RootAddress}/createfaceid/", content).Result;
            }
        }

        static private HttpResponseMessage DeleteFaceID(string faceID)
        {
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                client.BaseAddress = new Uri(BaseAddress);

                // 実行
                return client.DeleteAsync($@"/{RootAddress}/deletefaceid/{faceID}").Result;
            }
        }
        
        static private HttpResponseMessage AddFace(string faceID, List<string> images)
        {
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                client.BaseAddress = new Uri(BaseAddress);

                // ファイル添付
                images.ForEach(img => {
                    img.ToString();
                    var fileContent = new ByteArrayContent(File.ReadAllBytes($@"{AppDomain.CurrentDomain.BaseDirectory}\Images\{img}"));
                    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = img
                    };
                    content.Add(fileContent);
                });

                // 実行
                return client.PostAsync($@"/{RootAddress}/addface/{faceID}", content).Result;
            }
        }

        static private HttpResponseMessage UpdateFace(string faceID, string faceSubID, List<string> images)
        {
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                client.BaseAddress = new Uri(BaseAddress);

                // ファイル添付
                images.ForEach(img => {
                    img.ToString();
                    var fileContent = new ByteArrayContent(File.ReadAllBytes($@"{AppDomain.CurrentDomain.BaseDirectory}\Images\{img}"));
                    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = img
                    };
                    content.Add(fileContent);
                });

                // 実行
                return client.PutAsync($@"/{RootAddress}/updateface/{faceID}/{faceSubID}", content).Result;
            }
        }

        static private HttpResponseMessage DeleteFace(string faceID, string faceSubID)
        {
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                client.BaseAddress = new Uri(BaseAddress);

                // 実行
                return client.DeleteAsync($@"/{RootAddress}/deleteface/{faceID}/{faceSubID}").Result;
            }
        }

        static private HttpResponseMessage GetFaceID(string faceID)
        {
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                client.BaseAddress = new Uri(BaseAddress);

                // 実行
                return client.GetAsync($@"/{RootAddress}/getfaceid/{faceID}").Result;
            }
        }

        static private HttpResponseMessage CompareFace(List<string> images)
        {
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                client.BaseAddress = new Uri(BaseAddress);

                // ファイル添付
                images.ForEach(img => {
                    img.ToString();
                    var fileContent = new ByteArrayContent(File.ReadAllBytes($@"{AppDomain.CurrentDomain.BaseDirectory}\Images\{img}"));
                    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = img
                    };
                    content.Add(fileContent);
                });

                // 実行
                return client.PostAsync($@"/{RootAddress}/compareface/", content).Result;
            }
        }
    }
}
