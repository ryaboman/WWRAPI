using System;
using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace WWRAPI {

    public class RAPI {

        private ISerializable serializer;

        private bool busyFlag = false;
        public bool requesting;

        public delegate void ChangeStateTask(RAPI value);

        //Событие по завершению запроса
        public event ChangeStateTask RequestDone;

        public HttpStatusCode statusCode = HttpStatusCode.BadRequest;

        public CookieCollection cookies = null;

        private string pathToFileCookie;

        public string APP_PATH;

        private HttpClient client = null;
        private HttpClientHandler handler = null;

        private string responseData;



        public RAPI(string pathToFileCookie) {
            this.pathToFileCookie = pathToFileCookie;
            ReadCookieFile();
            OpenConnection(null);
        }

        public RAPI(string pathToFileCookie, Dictionary<string, string> headerOptions) {
            this.pathToFileCookie = pathToFileCookie;
            ReadCookieFile();
            OpenConnection(headerOptions);
        }

        public string GetRawData() {
            return responseData;
        }

        public void SetSerializer(ISerializable serializer) {
            this.serializer = serializer;
        }

        public bool isBusy() {
            return busyFlag;
        }

        public ResponseDataType GetDeserializeObject<ResponseDataType>() {
            // Десериализация полученного JSON-объекта
            ResponseDataType responseData;
            serializer.Deserialize<ResponseDataType>(this.responseData, out responseData);
            return responseData;
        }

        public void GetDeserializeObject<ResponseDataType>(out ResponseDataType responseData) {
            // Десериализация полученного JSON-объекта
            serializer.Deserialize<ResponseDataType>(this.responseData, out responseData);
        }

        private bool OpenConnection(Dictionary<string, string> headerOptions) {
            handler = new HttpClientHandler();

            handler.CookieContainer = new CookieContainer();

            if (cookies != null) {
                handler.CookieContainer.Add(cookies);
            }

            client = new HttpClient(handler);

            if (headerOptions != null) {
                foreach (var pairKeyValue in headerOptions) {
                    if ((pairKeyValue.Key != "Content-Type") && (pairKeyValue.Key != "Content-Length")) {
                        client.DefaultRequestHeaders.Add(pairKeyValue.Key, pairKeyValue.Value);
                    }
                }
            }
            

            return true;
        }

        public void CloseConnection() {


        }

        //Функция авторизации пользователя на web ресурсе
        //И получения куков авторизации
        //Эту функцию нужно будет удалить. На metanit нужно почитать про то как какие функции сейчас лучше использовать.
        public async void Authentication<RequestDataType>(string pathAPI, RequestDataType requestData, Dictionary<string, string> headerOptions) {

            cookies = null;

            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(APP_PATH + pathAPI);

                request.Method = "POST"; // для отправки используется метод Post
                request.CookieContainer = new CookieContainer();
                request.AllowAutoRedirect = false;

                string data = serializer.Serialize(requestData);
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(data);
                // устанавливаем тип содержимого - параметр ContentType
                request.ContentType = "application/x-www-form-urlencoded";
                foreach(var pairKeyValue in headerOptions) {
                    request.Headers.Add(pairKeyValue.Key, pairKeyValue.Value);
                }
                
                // Устанавливаем заголовок Content-Length запроса - свойство ContentLength
                request.ContentLength = byteArray.Length;

                //записываем данные в поток запроса
                using (Stream dataStream = request.GetRequestStream()) {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }


                //Получаем ответ
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                statusCode = response.StatusCode;

                using (Stream stream = response.GetResponseStream()) {

                    using (StreamReader reader = new StreamReader(stream)) {

                        //При авторизации нам не важен ответ echo от сервера
                        responseData = reader.ReadToEnd();

                    }

                    cookies = response.Cookies;
                    WriteCookieFile(response.Cookies);                    

                }

                response.Close();
            }
            catch (WebException ex) {
                if (ex.Response == null)
                    throw;
                statusCode = ((HttpWebResponse)ex.Response).StatusCode;
            }


        }

        //Функция получения данных json с сервера
        public async Task PostRequest<RequestDataType>(string pathAPI, RequestDataType requestData, Dictionary<string, string> headerOptions = null) {

            busyFlag = true;

            try {
                string pathToAplicationAPI = APP_PATH + pathAPI;

                string json = serializer.Serialize(requestData);
                HttpContent requestContent = new StringContent(json);

                if (headerOptions != null) {
                    foreach (var pairKeyValue in headerOptions) {
                        if (pairKeyValue.Key == "Content-Type") {
                            requestContent.Headers.ContentType = new MediaTypeHeaderValue(pairKeyValue.Value);
                        }
                        else if (pairKeyValue.Key == "Content-Length") {

                        }
                    }
                }

                //client.Timeout = TimeSpan.FromSeconds(2);

                HttpResponseMessage response = await client.PostAsync(pathToAplicationAPI, requestContent);

                var responseText = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(responseText);

                statusCode = response.StatusCode;

                

                cookies = handler.CookieContainer
                    .GetCookies(new Uri(pathToAplicationAPI));

                WriteCookieFile(cookies);

                responseData = response.Content.ReadAsStringAsync().Result;


            }
            catch (ArgumentOutOfRangeException ex) {
                Console.WriteLine("Время запроса вышло 1!");
            }
            catch (InvalidOperationException ex) {
                Console.WriteLine("Время запроса вышло 2!");
            }
            catch (TaskCanceledException ex) {
                Console.WriteLine("Время запроса вышло 3!");

                Console.WriteLine();
            }
            catch (Exception ex) {
                Console.WriteLine("Что-то случилось!");
            }
            finally {
                RequestDone?.Invoke(this);
            }

            busyFlag = false;
        }

        //Функция получения данных json с сервера
        public async Task GetRequest(string pathAPI, Dictionary<string, string> headerOptions = null) {
            busyFlag = true;

            try {
                string pathToAplicationAPI = APP_PATH + pathAPI;

                using (var handler = new HttpClientHandler()) {

                    handler.CookieContainer = new CookieContainer();

                    if (cookies != null) {
                        handler.CookieContainer.Add(cookies);
                    }

                    //handler.

                    using (var client = new HttpClient(handler)) {

                        var request = new HttpRequestMessage(HttpMethod.Get, pathToAplicationAPI);

                        if (headerOptions != null) {
                            foreach (var pairKeyValue in headerOptions) {
                                if ((pairKeyValue.Key != "Content-Type") && (pairKeyValue.Key != "Content-Length")) {
                                    client.DefaultRequestHeaders.Add(pairKeyValue.Key, pairKeyValue.Value);
                                }
                            }
                        }

                        HttpResponseMessage response = await client.SendAsync(request);


                        statusCode = response.StatusCode;

                        cookies = handler.CookieContainer
                            .GetCookies(new Uri(pathToAplicationAPI));

                        WriteCookieFile(cookies);

                        string responseStr = response.Content.ReadAsStringAsync().Result;

                        responseData = await response.Content.ReadAsStringAsync();
                    }


                }


            }
            catch (ArgumentOutOfRangeException ex) {
                Console.WriteLine("Время запроса вышло 1!");
            }
            catch (InvalidOperationException ex) {
                Console.WriteLine("Время запроса вышло 2!");
            }
            catch (TaskCanceledException ex) {
                Console.WriteLine("Время запроса вышло 3!");

                Console.WriteLine();
            }
            catch {
                Console.WriteLine("Что-то случилось!");
            }

            RequestDone?.Invoke(this);

            busyFlag = false;
        }

        //Функция скачивания файла с сервера
        public async Task<string> GetFileFromServerToAsync(string pathAPI, string pathToWrite) {

            busyFlag = true;

            string pathSave = String.Empty;

            try {

                string pathToAplicationAPI = APP_PATH + pathAPI;

                using (var handler = new HttpClientHandler()) {

                    handler.CookieContainer = new CookieContainer();
                    
                    if (cookies != null) {
                        handler.CookieContainer.Add(cookies);
                    }



                    using (var client = new HttpClient(handler)) {                        
                        var response = await client.GetAsync(pathToAplicationAPI);
                        //client.
                        
                        statusCode = response.StatusCode;

                        //var t = response.Content.Headers;

                        string fileName = response.Content.Headers.ContentDisposition.FileName;

                        

                        pathSave = Path.Combine(pathToWrite, fileName);

                        using (var fs = new FileStream(pathSave, FileMode.CreateNew) ) {

                            await response.Content.CopyToAsync(fs);

                        }

                    }



                }


            }
            catch (ArgumentOutOfRangeException ex) {
                Console.WriteLine("Время запроса вышло 1!");
            }
            catch (InvalidOperationException ex) {
                Console.WriteLine("Время запроса вышло 2!");
            }
            catch (TaskCanceledException ex) {
                Console.WriteLine("Время запроса вышло 3!");

                Console.WriteLine();
            }
            catch {
                Console.WriteLine("Что-то случилось!");
            }

            RequestDone?.Invoke(this);

            busyFlag = false;

            return pathSave;


        }

        //Функция загрузки файла на сервер
        public async Task<string> SendFileToServerToAsync(string pathAPI, string pathToFile) {

            busyFlag = true;

            string responseStr = String.Empty;

            try {

                string pathToAplicationAPI = APP_PATH + pathAPI;                

                using (var handler = new HttpClientHandler()) {

                    handler.CookieContainer = new CookieContainer();

                    if (cookies != null) {
                        handler.CookieContainer.Add(cookies);
                    }                    

                    using (var client = new HttpClient(handler)) {

                        var file = new StreamContent(new FileStream(pathToFile, FileMode.Open));
                        file.Headers.Add("Content-Disposition", @"form-data; name=file; filename=project_PWM_attiny85.zip");
                        file.Headers.Add("Content-Type", @"application/octet-stream");

                        MultipartFormDataContent content = new MultipartFormDataContent();

                        content.Add(file);

                        var response = await client.PostAsync(pathToAplicationAPI, content);                        

                        statusCode = response.StatusCode;

                        responseStr = response.Content.ReadAsStringAsync().Result;                    

                    }
                }
            }
            catch (ArgumentOutOfRangeException ex) {
                Console.WriteLine("Время запроса вышло 1!");
            }
            catch (InvalidOperationException ex) {
                Console.WriteLine("Время запроса вышло 2!");
            }
            catch (TaskCanceledException ex) {
                Console.WriteLine("Время запроса вышло 3!");

                Console.WriteLine();
            }
            catch {
                Console.WriteLine("Что-то случилось!");
            }

            RequestDone?.Invoke(this);

            busyFlag = false;

            return responseStr;
        }

        //Функция записи куков в файл
        private bool WriteCookieFile(CookieCollection cookies) {

            // создаем объект BinaryFormatter
            BinaryFormatter formatter = new BinaryFormatter();
            // получаем поток, куда будем записывать сериализованный объект
            using (FileStream fs = new FileStream(pathToFileCookie, FileMode.OpenOrCreate)) {
                formatter.Serialize(fs, cookies);

                return true;
            }

        }

        //Функция чтения куков из файла
        private void ReadCookieFile() {

            // создаем объект BinaryFormatter
            BinaryFormatter formatter = new BinaryFormatter();

            //десериализация из файла
            using (FileStream fs = new FileStream(pathToFileCookie, FileMode.OpenOrCreate)) {
                if(fs.Length != 0) {
                    cookies = (CookieCollection)formatter.Deserialize(fs);
                }
                else {
                    cookies = new CookieCollection();
                }
                               
            }
        }

    }

}
