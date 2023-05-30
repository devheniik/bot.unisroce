using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace tgbotconsole
{
    public class BotUnisroce
    {
        static TelegramBotClient botClient = new TelegramBotClient("5805249008:AAGuiKKF9GI3A5yiF9ApGAWWdUEoG3xKuH8");
        private static string url = "http://localhost:5173/api/";
        static HttpClient client = new HttpClient();
        
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }
        
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if(update.Type == UpdateType.Message)
            {
                OnMessageHandler(botClient, update);
            }
            
            if(update.Type == UpdateType.CallbackQuery)
            {
                
                OnCallbackQueryHandler(botClient, update);
            }
        }

        public static async void OnCallbackQueryHandler(ITelegramBotClient botClient, Update update)
        {
            
            var User = await GetUserByChatId(update.CallbackQuery.From.Id.ToString());
            
            var Action = update.CallbackQuery.Data.Split(".")[0];
            var Id = update.CallbackQuery.Data.Split(".")[1];

            if (Action == "SET_GROUP")
            {
                User.GroupId = Id;
                await MakePutRequest<User>(User.ChatId + "/users/update", User);
                User = await GetUserByChatId(update.CallbackQuery.From.Id.ToString());
                var Subjects = await MakeGetRequest<List<string>>(User.ChatId + "/subjects/import/show");
                
                var SubjectsMessage = "Предмети вашої группи: \n";
                foreach (var SubjectName in Subjects)
                {
                    SubjectsMessage += SubjectName + " \n";
                }
                
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                InlineKeyboardButton button = new InlineKeyboardButton("Імпортувати предмети з моеї группи") { CallbackData = "IMPORT_SUBJECTS.NULL", Text = "Імпортувати предмети з моеї группи"};
                InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button }; 
                list.Add(row);
                var inline = new InlineKeyboardMarkup(list);
                botClient.SendTextMessageAsync(User.ChatId, SubjectsMessage, replyMarkup: inline);
                
                return;
                
            } else if(Action == "IMPORT_SUBJECTS")
            {
                var status = await MakePostRequest<int>(User.ChatId + "/subjects/import/apply");

                if (status == 1)
                {
                    await botClient.SendTextMessageAsync(User.ChatId, "Предмети успішно імпортовані \n /subjects - побачити ваші предмети");
                } else
                {
                    await botClient.SendTextMessageAsync(User.ChatId, "Помилка імпортування предметів");
                }

                return;
            } else if (Action == "UPDATE_SUBJECT")
            {
                User.SubjectId = Id;
                User.CurrentAction = "UPDATE_SUBJECT";
                await MakePutRequest<User>(User.ChatId + "/users/update", User);
                User = await GetUserByChatId(update.CallbackQuery.From.Id.ToString());
                await botClient.SendTextMessageAsync(User.ChatId, "Введіть нову назву предмету");
            }  else if (Action == "SELECT_SUBJECT")
            {
                User.SubjectId = Id;
                User.CurrentAction = "CREATE_EVENT";
                await MakePutRequest<User>(User.ChatId + "/users/update", User);
                User = await GetUserByChatId(update.CallbackQuery.From.Id.ToString());
                await botClient.SendTextMessageAsync(User.ChatId, "Введіть назву роботи та оцінку через @" + "(наприклад: Лабораторна робота 1 @ 5)");
            } else if (Action == "DELETE_SUBJECT")
            {
                var status = await MakeDeleteRequest(User.ChatId + "/subjects/" + Id);

                if (status)
                {
                    await botClient.SendTextMessageAsync(User.ChatId, "Предмет успішно видалено \n /subjects - побачити ваші предмети");
                }
                else
                {
                    await botClient.SendTextMessageAsync(User.ChatId, "Помилка видалення предмета");
                }
            } else if (Action == "DELETE_EVENT")
            {
                var status = await MakeDeleteRequest(User.ChatId + "/events/" + Id);

                if (status)
                {
                    await botClient.SendTextMessageAsync(User.ChatId, "Подію успішно видалено \n /events - побачити ваші події");
                }
                else
                {
                    await botClient.SendTextMessageAsync(User.ChatId, "Помилка видалення події");
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(User.ChatId, "Кпопка вже застаріла(");
            }
        }
        
        public static async void OnMessageHandler(ITelegramBotClient botClient, Update update)
        {
            var user = await GetUserByChatId(update.Message.Chat.Id.ToString());
            var message = update.Message;
            if (message.Text.ToLower() == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat, "Приємно познайомитись, " + message.Chat.FirstName + " " + message.Chat.LastName + "!");
                SendCommands(message.Chat);
                return;
            } else if (message.Text.ToLower() == "/fiot")
            {
                var groups = await MakeGetRequest<List<Group>>( "groups");
                
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                foreach (var group in groups)
                {
                    InlineKeyboardButton button = new InlineKeyboardButton(group.name) { CallbackData = "SET_GROUP." + group.id, Text = group.name };
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button }; 
                    list.Add(row);
                    
                }
                
                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(message.Chat, "Оберіть группу щоб побачити предмети", replyMarkup: inline);
                
                return;
            } else if (message.Text.ToLower() == "/subjects")
            {
                var Subjects = await MakeGetRequest<List<Subject>>(user.ChatId + "/subjects");
                
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                foreach (var Subject in Subjects)
                {
                    InlineKeyboardButton button = new InlineKeyboardButton(Subject.Name) { CallbackData = "UPDATE_SUBJECT." + Subject.Id, Text = Subject.Name };
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button }; 
                    list.Add(row);
                    
                }
                
                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(message.Chat, "Предмети вашої группи: \n Щоб редагувати предмет натисніть на нього \n", replyMarkup: inline);

                
                return;
            } else if (message.Text.ToLower() == "/events")
            {
                var Events = await MakeGetRequest<List<Event>>(user.ChatId + "/events");
                
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                foreach (var _Event in Events)
                {
                    var Text = _Event.Name + " " + _Event.Mark + " (" + _Event.SubjectName + ")";
                    InlineKeyboardButton button = new InlineKeyboardButton(Text) { CallbackData = "DELETE_EVENT." + _Event.Id, Text = Text };
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button }; 
                    list.Add(row);
                    
                }
                
                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(message.Chat, "Роботи: \n Щоб видалити роботу натисніть на неї \n", replyMarkup: inline);

                
                return;
            } 
            else if (message.Text.ToLower() == "/create_subjects")
            {
                user.CurrentAction = "CREATE_SUBJECT";
                await MakePutRequest<User>(user.ChatId + "/users/update", user);
                user = await GetUserByChatId(user.ChatId.ToString());
                await botClient.SendTextMessageAsync(user.ChatId, "Введіть назву предмету");
            } 
            else if (message.Text.ToLower() == "/create_events")
            {
                var Subjects = await MakeGetRequest<List<Subject>>(user.ChatId + "/subjects");
                
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                foreach (var Subject in Subjects)
                {
                    InlineKeyboardButton button = new InlineKeyboardButton(Subject.Name) { CallbackData = "SELECT_SUBJECT." + Subject.Id, Text = Subject.Name };
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button }; 
                    list.Add(row);
                    
                }
                
                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(message.Chat, "Щоб створити роботу вам потрібно обрати предмет: \n", replyMarkup: inline);

                
                return;
            }
            else if (message.Text.ToLower() == "/marks")
            {
                 var SubjectSummaries = await MakeGetRequest<List<SubjectSummary>>(user.ChatId + "/subjects/summary");
                 
                 string Text = "--------------- \n";
                 foreach (var SubjectSummary in SubjectSummaries)
                 { 
                     Text += SubjectSummary.Name + ": " + SubjectSummary.Total + "\n";
                     Text += "----------------- \n";
                 }
                 
                 await botClient.SendTextMessageAsync(message.Chat, Text);
            }
            else if (message.Text.ToLower() == "/delete_subjects")
            {
                var Subjects = await MakeGetRequest<List<Subject>>(user.ChatId + "/subjects");
                
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                foreach (var Subject in Subjects)
                {
                    InlineKeyboardButton button = new InlineKeyboardButton(Subject.Name) { CallbackData = "DELETE_SUBJECT." + Subject.Id, Text = Subject.Name };
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button }; 
                    list.Add(row);
                    
                }
                
                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(message.Chat, "Предмети вашої группи: \n Щоб видалити предмет натисніть на нього \n", replyMarkup: inline);

                
                return;
            } else if (message.Text.ToLower() == "/help")
            {
                SendCommands(message.Chat);
                return;
            }
            else
            {
                if (user.CurrentAction == "UPDATE_SUBJECT")
                {
                    var subject = await MakePutRequest<Subject>(user.ChatId + "/subjects/" + user.SubjectId, new UpdateSubjectRequest() { Name = message.Text});

                    if (subject != null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Предмет успішно оновлено");
                        user.CurrentAction = "";
                        user.SubjectId = null;
                        await MakePutRequest<User>(user.ChatId + "/users/update", user);
                        await botClient.SendTextMessageAsync(message.Chat, "Оберіть команду /subjects щоб побачити всі предмети");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Помилка оновлення предмета");
                    }
                } else if (user.CurrentAction == "CREATE_EVENT")
                {
                    if (!message.Text.Contains(" "))
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Введіть назву роботи та оцінку через '@'");
                        return;
                    }
                    
                    string[] parts = message.Text.Split('@');

                    string beforeSpace = parts[0];
                    if (string.IsNullOrEmpty(beforeSpace))
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Введіть назву роботи та оцінку через '@'");
                        return;
                    }
                    
                    string afterSpace = parts[1].Trim();;
                    double number;
                    if (!double.TryParse(afterSpace, out number))
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Введіть назву роботи та оцінку через '@'");
                        return;
                    }
                    
                    var _Event = await MakePostRequest<Subject>(user.ChatId + "/events", new CreateEventRequest() { Name = beforeSpace, Mark = number, SubjectId = user.SubjectId});

                    if (_Event != null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Робота успішно додана");
                        user.CurrentAction = "";
                        user.SubjectId = null;
                        await MakePutRequest<User>(user.ChatId + "/users/update", user);
                        await botClient.SendTextMessageAsync(message.Chat, "Оберіть команду /events щоб побачити всі роботи");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Помилка оновлення предмета");
                    }
                } 
                else if (user.CurrentAction == "CREATE_SUBJECT")
                {
                    var subject = await MakePostRequest<Subject>(user.ChatId + "/subjects", new CreateSubjectRequest() { Name = message.Text});

                    if (subject != null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Предмет успішно додано");
                        user.CurrentAction = "";
                        user.SubjectId = null;
                        await MakePutRequest<User>(user.ChatId + "/users/update", user);
                        await botClient.SendTextMessageAsync(message.Chat,
                            "Оберіть команду /subjects щоб побачити всі предмети");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Помилка додавання предмета");
                    }
                }
                else if (user.CurrentAction == "CREATE_SUBJECT")
                {
                    var subject = await MakePostRequest<Subject>(user.ChatId + "/events", new CreateSubjectRequest() { Name = message.Text});

                    if (subject != null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Роботу успішно додано");
                        user.CurrentAction = "";
                        await MakePutRequest<User>(user.ChatId + "/users/update", user);
                        await botClient.SendTextMessageAsync(message.Chat,
                            "Оберіть команду /subjects щоб побачити всі предмети");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Помилка додавання предмета");
                    }
                }
            }
            
            // await botClient.SendTextMessageAsync(message.Chat, "Оберіть команду /help щоб побачити всі команди");
        }

        public static async void SendCommands(Chat Chat)
        {
            await botClient
                .SendTextMessageAsync(
                    Chat, 
                    "/start - Розпочати роботу\n " +
                    "/fiot - Побичити группи ФІОТа \n " +
                    "/create_subjects - додати предмет \n " +
                    "/delete_subjects - видалити предмет \n " +
                    "/subjects - побачити ваші предмети \n " +
                    "/create_events - додати роботу \n " +
                    "/events - побачити ваші роботи \n " +
                    "/marks - побачити табицю оцінок \n " +
                    "/help - побачити команди");
        }
        
        public static async Task<User> GetUserByChatId(string chatId)
        {
            var UserExist = await MakeGetRequest<string>(chatId + "/users/check");

            if (UserExist == "true")
            {
                var ExistUser = await MakeGetRequest<User>(chatId + "/users");
                return ExistUser;
            }
            
            var user = await MakePostRequest<User>("register",new RegisterUser(chatId));

            return user;
        }
        
        public static async Task<T> MakePostRequest<T>(string apiUrl, dynamic body = null)
        {
            var jsonRequest = "";
            if (body != null)
            {
                jsonRequest = JsonConvert.SerializeObject(body);
            }
            
            HttpContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url + apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response);
                throw new Exception("Api post error");
            }
            
            string jsonResponse = await response.Content.ReadAsStringAsync();
            T DeserializedObject = JsonConvert.DeserializeObject<T>(jsonResponse);

            return DeserializedObject;
        }
        public static async Task<T> MakePutRequest<T>(string apiUrl, dynamic body = null)
        {
            var jsonRequest = "";
            if (body != null)
            {
                jsonRequest = JsonConvert.SerializeObject(body);
            }
            HttpContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PutAsync(url + apiUrl, content);
            

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response);
                throw new Exception("Api put error");
            }
            
            string jsonResponse = await response.Content.ReadAsStringAsync();
            T DeserializedObject = JsonConvert.DeserializeObject<T>(jsonResponse);

            return DeserializedObject;
        }
        
        public static async Task<T> MakeGetRequest<T>(string apiUrl)
        {
            HttpResponseMessage response = await client.GetAsync(url + apiUrl);

            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response);
                throw new Exception("Api get error");
            }
            
            string jsonResponse = await response.Content.ReadAsStringAsync();
            T DeserializedObject = JsonConvert.DeserializeObject<T>(jsonResponse);

            return DeserializedObject;
        }
        
        public static async Task<bool> MakeDeleteRequest(string apiUrl)
        {
            HttpResponseMessage response = await client.DeleteAsync(url + apiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response);
                throw new Exception("Api delete error");
            }

            return true;
        }
        
        
        static void Main(string[] args)
        {
            Console.WriteLine("Запущен бот " + botClient.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = {  }, // receive all update types
            };
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            
            Console.ReadLine();
        }
    }
    
    public class User
    {
        public Guid Id { get; set; }
        public string ChatId { get; set; }
        public string CurrentAction { get; set; }
        public string GroupId { get; set; }
        public string SubjectId { get; set; }
        public string TempEventName { get; set; }
    }
    
    public class Group
    {
        public string id { get; set; }
        public string name { get; set; }
        public string faculty { get; set; }
    }
    
    public class RegisterUser
    {
        public string ChatId { get; set; }
        
        public RegisterUser(string ChatId)
        {
            this.ChatId = ChatId;
        }
    }
    
    public class Subject
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
    }
    
    public class UpdateSubjectRequest
    {
        public string Name { get; set; }
    }
    
    public class CreateSubjectRequest
    {
        public string Name { get; set; }
    }
    
    public class CreateEventRequest
    {
        public string Name { get; set; }
        public double Mark { get; set; }
        public string SubjectId { get; set; }
    }
    
    public class Event
    {
        public Guid Id { get; set; }
        public Guid SubjectId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public double Mark { get; set; }
        
        public string? SubjectName { get; set; }
    }
    
    public class SubjectSummary
    {
    
        public string Name { get; set; }
        public double Total { get; set; }
    }
}