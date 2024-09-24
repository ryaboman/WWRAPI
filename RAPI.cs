using System;
using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;

namespace WWRAPI {

    public class RAPI<ResponseDataType> {

        public delegate void ChangeStateTask(RAPI<ResponseDataType> value);

        //Событие по завершению запроса
        public event ChangeStateTask RequestDone;

        public HttpStatusCode statusCode = HttpStatusCode.BadRequest;

        public static CookieCollection cookies = null;

        private string pathToFileCookie;

        public string APP_PATH;

        private HttpContent content = null;

        public ResponseDataType responseData;

        public bool requesting;

        public RAPI(string pathToFileCookie){
            this.pathToFileCookie = pathToFileCookie;
            ReadCookieFile();
        }

        //Функция авторизации пользователя на web ресурсе
        //И получения куков авторизации
        public async void Authentication<RequestDataType>(string pathAPI, RequestDataType requestData) {

            cookies = null;

            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(APP_PATH + pathAPI);

                request.Method = "POST"; // для отправки используется метод Post
                request.CookieContainer = new CookieContainer();
                request.AllowAutoRedirect = false;

                string data = JsonConvert.SerializeObject(requestData);
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(data);
                // устанавливаем тип содержимого - параметр ContentType
                request.ContentType = "application/x-www-form-urlencoded";
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

                        //При авторизации нам не важет ответ echo от сервера
                        string responseStr = reader.ReadToEnd();

                        responseData = JsonConvert.DeserializeObject<ResponseDataType>(responseStr);

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
        public async Task PostRequest<RequestDataType>(string pathAPI, RequestDataType requestData) {

            try {
                string pathToAplicationAPI = APP_PATH + pathAPI;

                using (var handler = new HttpClientHandler()) {

                    handler.CookieContainer = new CookieContainer();

                    if (cookies != null) {
                        handler.CookieContainer.Add(cookies);
                    }


                    using (var client = new HttpClient(handler)) {

                        string json = JsonConvert.SerializeObject(requestData);
                        HttpContent requestContent = new StringContent(json);

                        //client.Timeout = TimeSpan.FromSeconds(2);

                        HttpResponseMessage response = await client.PostAsync(pathToAplicationAPI, requestContent);

                        statusCode = response.StatusCode;

                        cookies = handler.CookieContainer
                            .GetCookies(new Uri(pathToAplicationAPI));

                        WriteCookieFile(cookies);

                        string responseStr = response.Content.ReadAsStringAsync().Result;
                        // Десериализация полученного JSON-объекта
                        responseData = JsonConvert.DeserializeObject<ResponseDataType>(responseStr);
                        
                        //"{\"login\":\"user\",\"password\":\"123456\",\"statusAuthentication\":true,\"errorAuthentication\":[]}"
                        
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
        }

        //Функция получения данных json с сервера
        //Переименовать в POST
        public async Task GetRequest(string pathAPI) {

            try {
                string pathToAplicationAPI = APP_PATH + pathAPI;

                using (var handler = new HttpClientHandler()) {

                    handler.CookieContainer = new CookieContainer();

                    if (cookies != null) {
                        handler.CookieContainer.Add(cookies);
                    }


                    using (var client = new HttpClient(handler)) {

                        HttpResponseMessage response = await client.GetAsync(pathToAplicationAPI);

                        statusCode = response.StatusCode;

                        cookies = handler.CookieContainer
                            .GetCookies(new Uri(pathToAplicationAPI));

                        WriteCookieFile(cookies);

                        string responseStr = response.Content.ReadAsStringAsync().Result;

                        // Десериализация полученного JSON-объекта
                        responseData = JsonConvert.DeserializeObject<ResponseDataType>(responseStr);

                        //"{\"login\":\"user\",\"password\":\"123456\",\"statusAuthentication\":true,\"errorAuthentication\":[]}"

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
        }

        //Функция скачивания файла с сервера
        public async Task<string> GetFileFromServerToAsync(string pathAPI, string pathToWrite) {

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

            return pathSave;


        }

        //Функция загрузки файла на сервер
        public async Task<string> SendFileToServerToAsync(string pathAPI, string pathToFile) {
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
