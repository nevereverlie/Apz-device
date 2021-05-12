using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Newtonsoft.Json;


namespace Apz_device
{
    class Program
    {
        private const string API_URL = "https://localhost:5001/";
        private const int MINUTE = 60_000;
        private static LoginUser userToLogin { get; set; }
        private static OasMedication[] medications { get; set; }
        private static ResponseToken token { get; set; }

        static async Task Main(string[] args)
        {
            #region SETUP
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            bool isLoggedIn = false;

            using var client = new HttpClient();
            client.DefaultRequestHeaders
                  .Accept
                  .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            do
            {
                LoginUser userToLogin = GetUserData();

                HttpResponseMessage loginResult = await LoginUser(userToLogin, client);

                if (loginResult.IsSuccessStatusCode)
                {
                    isLoggedIn = true;
                    token = loginResult.Content.ReadAsAsync<ResponseToken>().Result;

                    HttpResponseMessage medicationsResult = await client.GetAsync(API_URL + "medications");
                    medications = medicationsResult.Content.ReadAsAsync<OasMedication[]>().Result;
                    foreach (OasMedication medication in medications)
                    {
                        Console.WriteLine(medication.MedicineName);
                    }
                }
                else
                {
                    Console.WriteLine("\nПомилка: " + loginResult.Content.ReadAsStringAsync().Result + ". Введіть дані знову.\n");
                }

            } while (!isLoggedIn);

            int userId = token.UserId;

            GC.Collect();

            #endregion SETUP

            #region LOOP
            //Timer timer = new Timer(isUserWorking, null, 0, MINUTE);

            Console.ReadLine();
            #endregion LOOP
        }

        public static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                {
                    Console.Write("*");
                    password += info.KeyChar;
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        // remove one character from the list of password characters
                        password = password.Substring(0, password.Length - 1);
                        // get the location of the cursor
                        int pos = Console.CursorLeft;
                        // move the cursor to the left by one character
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        // replace it with space
                        Console.Write(" ");
                        // move the cursor to the left by one character again
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }
                info = Console.ReadKey(true);
            }
            // add a new line because user pressed enter at the end of their password
            Console.WriteLine();
            return password;
        }

        private static async Task<HttpResponseMessage> LoginUser(LoginUser userToLogin, HttpClient client)
        {
            var json = JsonConvert.SerializeObject(userToLogin);
            var userData = new StringContent(json, Encoding.UTF8, "application/json");

            var loginResult = await client.PostAsync(API_URL + "auth/login", userData);
            return loginResult;
        }

        private static LoginUser GetUserData()
        {
            Console.Write("E-mail: ");
            string email = Console.ReadLine();
            Console.Write("\nПароль: ");
            string password = ReadPassword();

            LoginUser userToLogin = new LoginUser
            {
                Email = email,
                Password = password
            };
            return userToLogin;
        }
    }
}
