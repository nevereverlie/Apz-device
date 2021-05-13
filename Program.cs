using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Drawing;

namespace Apz_device
{
    class Program
    {
        private const string API_URL = "https://localhost:5001/";
        private const int MINUTE = 60_000;
        private static string AnimalType { get; set; } = "Cat";
        private static OasMedication[] Medications { get; set; }
        private static ResponseToken Token { get; set; }

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
                    Token = loginResult.Content.ReadAsAsync<ResponseToken>().Result;
                    Console.WriteLine("Вхід до системи ");
                }
                else
                {
                    Console.WriteLine("\nПомилка: " + loginResult.Content.ReadAsStringAsync().Result + ". Введіть дані знову.\n");
                }

            } while (!isLoggedIn);

            int userId = Token.UserId;

            GC.Collect();

            #endregion SETUP

            #region LOOP
            Timer timer = new Timer(isMedicationNeeded, null, 0, MINUTE);

            Console.ReadLine();
            #endregion LOOP
        }

        private async static void isMedicationNeeded(Object state)
        {
            using var client = new HttpClient();

            HttpResponseMessage medicationsResult = await client.GetAsync(API_URL + "Medications");
            Medications = medicationsResult.Content.ReadAsAsync<OasMedication[]>().Result;

            try
            {
                foreach (OasMedication medication in Medications)
                {
                    if (medication.MedicationTime == DateTime.Now.ToString("HH:mm"))
                    {
                        try
                        {
                            var extensions = new string[] { ".png", ".jpg" };
                            var dir = new DirectoryInfo("./test-data");
                            var rgFiles = dir.GetFiles("*.*").Where(f => extensions.Contains(f.Extension.ToLower()));
                            Random R = new Random();
                            Image image = Image.FromFile(rgFiles.ElementAt(R.Next(0, rgFiles.Count())).FullName);


                            HttpContent content = new ByteArrayContent(image.ToByteArray());
                            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                            var getDistanceResult = await client.PostAsync(API_URL + $"iot/detectDistance/{AnimalType}", content);

                            bool isAnimalAvailable = getDistanceResult.Content.ReadAsAsync<bool>().Result;
                            if (isAnimalAvailable)
                            {
                                Console.WriteLine($"Видати тварині {AnimalType} медикамент {medication.MedicineName} у виді {medication.MedicationType} мірою в {medication.MedicationAmount}, час: {medication.MedicationTime}");
                            }
                            else
                            {
                                Console.WriteLine("Тварина не виявлена або дистанція до неї надто велика...");
                            }
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Помилка: не вдалося перевірити присутність тварини...");
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Помилка при отриманні тестових даних...");
            }
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

        private static async Task<HttpResponseMessage> LoginUser(LoginUser UserToLogin, HttpClient client)
        {
            var json = JsonConvert.SerializeObject(UserToLogin);
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

            LoginUser UserToLogin = new LoginUser
            {
                Email = email,
                Password = password
            };
            return UserToLogin;
        }
    }

    public static class ImageConverter
    {
        public static byte[] ToByteArray(this Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, image.RawFormat);
                return ms.ToArray();
            }
        }
    }
}
