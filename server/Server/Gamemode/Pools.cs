using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace ProtonServer
{
    public partial class Gamemode
    {
        private List<(Vector3 min, Vector3 max)> greenZones = new List<(Vector3, Vector3)>{
            (new Vector3(652.8f, 0, -868.2f), new Vector3(889.8f, 0, -664f)),
            (new Vector3(-783.7f, 0, -74.8f), new Vector3(-409.7f, 0, 315.4f)),
            (new Vector3(82.4f, 0, -647f), new Vector3(457.58f, 0, -301f)),
            (new Vector3(524.6f, 0, 335f), new Vector3(797f, 0, 625f)),
        };

        public enum DialogID
        {
            Default = -1,
            Help = 3,
            SpawnChoose = 4,
            CMDList = 5,
            AccountInfo = 6,
            Report = 7,
            ShootingForbidden = 8,

            EducationStart = 9,
            EducationEnd = 17,

            JobSelector = 31,
            MineshaftWorkStart = 32,
            MineshaftWorkEnd = 33,

            FrederikoHelpRequestDialog = 41,
            FrederikoHelpInfo = 42,
            FrederikoHelpChoose = 43,
            QuestMenu = 44,
            QuestInfo = 45,

            GPS = 71,

            CarRentChoose = 91,
            CarRentStop = 92,

            BusWorkStart = 101,
            BusWorkEnd = 102,
            BusWorkWaySelector = 103,

            AdminLogin = 111,
            AdminPlayerMenu = 112,
            AdminMessageToPlayer = 113,
            AdminGiveMoney = 114,
            AdminGiveAchievement = 115,
            ServerError = 116,
            AdminPanel = 117,
            AdminMusicChange = 118,

            HelloMessageDialog = 131,
            PromoCodeDialog = 132,
            EducationChooseDialog = 133,

        }

        public enum PickupID
        {
            Marker = 1,
            MineshaftJob = 2,
            Frederiko = 3,
            QuestHelper = 4,
            BusJob = 5,

            RentCar = 21,
        }

        public enum TextdrawID
        {
            Exception = -1,

        }

        public enum QuestID // "Истинный шахтёр", "Опять в бункер...", "Водитель адской повозки", "Обновка", "ПУЛЬТ ОТ ЯДЕРКИ", "Машина мечты", "Самый адекватный политик", "Этот прицел просто имба!"
        {
            Mineshafter,
            BombAliver,
            BusDriver,
            SkinUpdate,
            NuclearBombController,
            Car,
            Hofman,
            Sniper
        }

        public enum JobID
        {
            None = 0,
            Minechafter = 1,
            BusDriver = 3,
            ProductDriver = 5,
            Bomber = 8
        }

        private Dictionary<int, object[]> staticDialogInfo = new Dictionary<int, object[]>()
        {
            { -1, new object[] { "Заголовок Диалога Выбора", DialogType.CHOOSE, new string[] { "Пункт 0", "Пункт 1", "Пункт 2" } } },
            { -2, new object[] { "Заголовок Диалога Информации", DialogType.INFO, "Это диалог с информацией. В аргумент идёт строка, в ответ сервер получает лишь кнопку cancel" } },
            { -3, new object[] { "Заголовок Диалога Ввода", DialogType.INPUT, "Это диалог с информацией и вводом. В аргумент идёт строка, в ответ сервер получает значение поля ввода" } },

            { 1, new object[] { "Добро пожаловать!", DialogType.INFO, "Добро пожаловать! Вы зашли на Donbass RP. Для получения справки отправьте команду /help в чат." } },
            { 2, new object[] { "Пароль", DialogType.INPUT, "Введите пароль для раннего доступа к серверу." } },
            { 3, new object[] { "Помощь", DialogType.CHOOSE, new string[] { "Информация об аккаунте", "Список команд", "Обращение к администрации", "Обучение", "Работы" } } },
            { 4, new object[] { "Выбор спавна", DialogType.CHOOSE, new string[] { "Бомбецк", "Последняя точка до выхода" } } },
            { 5, new object[] { "Команды", DialogType.INFO, "/help - показывает справку о сервере\n/jobs - показывает список работ\n/report - отправляет отзыв создателям\n/pay <id игрока> <сумма> - дает игроку денег" } },
            { 6, new object[] { "Информация об аккаунте", DialogType.INFO, "Никнейм: {0}\nID: {1}\nДата регистрации: {2}\nДеньги: {3}$\nEXP: {4}" } },
            { 7, new object[] { "Обращение к администрации", DialogType.INPUT, "Введите свои предложения по улучшению сервера." } },
            { 8, new object[] { "Стрельба запрещена!", DialogType.INFO, "СТРЕЛЬБА В ЗЕЛЁНОЙ ЗОНЕ ЗАПРЕЩЕНА!!!" } },

            { 9, new object[] { "Обучение", DialogType.INFO, "Хорошо, сейчас ты пройдешь небольшое обучение!" } },
            { 10, new object[] { "1. Общение", DialogType.INFO, "Ты можешь общаться с игроками с помощью <color=green>текстового</color> и <color=green>голосового</color> чата." } },
            { 11, new object[] { "2. Модерация", DialogType.INFO, "Старайся не начинать бессмысленных споров в чатах, иначе получишь наказание!" } },
            { 12, new object[] { "3. Экономика", DialogType.INFO, "Чтобы заработать деньги устройся на работу <color=green>(/jobs)</color>. Чем выше твой опыт - тем круче твои возможности" } },
            { 13, new object[] { "4. Опыт", DialogType.INFO, "Опыт <color=green>(EXP)</color> можно заработать, проходя квесты. Увидеть уровень опыта можно в /help -> 1 пункт" } },
            { 14, new object[] { "5. Бомбежки", DialogType.INFO, "Сервер часто бомбят! Каждая пережитая тобой бомбежка увеличивает твой уровень опыта" } },
            { 15, new object[] { "6. Квесты", DialogType.INFO, "Для начального продвижения тебе сильно помогут <color=green>Квесты</color>. Чтобы получить их, подойди к персонажу <color=green>Михаил Фредерико</color> на спавне" } },
            { 16, new object[] { "7. Помощь", DialogType.INFO, "Если тебе будет нужна помощь, запомни главную команду - <color=green>/help</color>" } },
            { 17, new object[] { "Обучение", DialogType.INFO, "Если ты готов начать играть, нажми <color=green>Далее</color>!" } },

            { 31, new object[] { "Работа", DialogType.CHOOSE, new string[] { "Шахтер (1 EXP)", "Водитель автобуса (3 EXP)", "Развозчик продуктов (5 EXP)", "Летчик-бомбардировщик (8 EXP)" } } },

            { 32, new object[] { "Работа шахтера", DialogType.INFO, "Вы хотите устроиться на работу шахтера?" } },
            { 33, new object[] { "Работа шахтера", DialogType.INFO, "Вы хотите уволиться с работы шахтера?" } },

            { 41, new object[] { "Диалог с Фредерико", DialogType.CHOOSE, new string[] { "Кто ты?", "Зачем ты здесь стоишь?", "Что мне делать?", "Как мне увеличить уровень?", "Как мне стать богатым?", "Где я могу постреляться?", "Как задонатить?", "Я хочу бомбить города", "Кто такой Игорь Гофман?", "Ты нюхаешь бебру?" } } },
            { 42, new object[] { "Диалог с Фредерико", DialogType.INFO, "Михаил Фредерико: {0}" } },
            { 43, new object[] { "Выбор помощи", DialogType.CHOOSE, new string[] { "Квесты", "Помощь по серверу" } } },
            { 44, new object[] { "Квесты", DialogType.CHOOSE, questInfo.Keys.ToArray() } },
            { 45, new object[] { "Квест", DialogType.INFO, "Название квеста: {0}\nВыполнен: {1}\nНаграда: {2}\nСуть квеста: {3}" } },

            { 71, new object[] { "GPS", DialogType.CHOOSE, new string[] { "Работы", "Бункеры", "Интересные места" } } },
            { 72, new object[] { "GPS - Работы", DialogType.CHOOSE, new string[] { } } },
            { 73, new object[] { "GPS - Бункеры", DialogType.CHOOSE, new string[] { } } },
            { 74, new object[] { "GPS - Интересные места", DialogType.CHOOSE, new string[] { } } },

            { 91, new object[] { "Аренда транспорта", DialogType.CHOOSE, new string[] { "Машина новичка (20$)" } } },
            { 92, new object[] { "Аренда транспорта", DialogType.INFO, "У вас уже есть арендованное транспортное средство. Вы хотите удалить его, чтобы арендовать новый транспорт?" } },

            { 101, new object[] { "Работа водителя автобуса", DialogType.INFO, "Вы хотите устроиться на работу водителя автобуса?" } },
            { 102, new object[] { "Работа водителя автобуса", DialogType.INFO, "Вы хотите уволиться с работы водителя автобуса?" } },
            { 103, new object[] { "Выбор маршрута", DialogType.CHOOSE, new string[] { "Окружной (2000$)" } } },

            { 111, new object[] { "Админ-авторизация", DialogType.INPUT, "Введите админ-пароль. Учтите, что при неправильном вводе ваш аккаунт получит блокировку на 1 час." } },
            { 112, new object[] { "Управление игроком", DialogType.CHOOSE, new string[] { "Телепортироваться к игроку", "Телепортировать к себе", "Отправить игроку сообщение", "Кикнуть игрока", "Забанить игрока", "Выдать игроку деньги", "Выдать игроку достижение" } } },
            { 113, new object[] { "Отправить сообщение игроку", DialogType.INPUT, "Введите сообщение для игрока" } },
            { 114, new object[] { "Выдать деньги игроку", DialogType.INPUT, "Введите сумму денег для игрока" } },
            { 115, new object[] { "Выдать достижение игроку", DialogType.INPUT, "Введите ID ачивки для игрока" } },
            { 116, new object[] { "СЕРВЕРНАЯ ОШИБКА", DialogType.INFO, "На сервере произошла ошибка.\nИмя: {0}\nСообщение: {1}\n" } },
            { 117, new object[] { "Админ-панель", DialogType.CHOOSE, new string[] { "Включить музыку у игроков", "Начать бомбардировку", "Отправить глобальное сообщение", "Кикнуть всех (кроме себя)", "Закрыть/Открыть сервер" } } },
            { 118, new object[] { "Смена музыки", DialogType.INPUT, "Введите имя файла музыки (пример: donbass.mp3)" } },

            { 131, new object[] { "Приветствие", DialogType.INFO, "Добро пожаловать в лучшую игру 2023 века - Симулятор Донбасса!" } },
            { 132, new object[] { "Промокод", DialogType.INPUT, "Введите промокод гостя если он у вас есть. На 4-ом уровне вы получите:\n<color=orange>10000 $ и Особенный скин на выбор</color>" } },
            { 133, new object[] { "Вы хотите пройти обучение?", DialogType.CHOOSE, new string[] { "Да, я только скачал игру", "Нет, я уже играл" } } },
        };
        private static Dictionary<string, string> questInfo = new Dictionary<string, string>
        {
            { "Сын шахтёра", "1 EXP, 1000$;Отправляйся на шахту (/jobs) и собери там 100 камней." },
            { "Опять в бункер...", "2 EXP;Переживи событие \"Бомбёжка города\". Бомбёжки происходят каждые 10-15 минут." },
            { "Водитель адской повозки", "1 EXP, 2000$;Заработай 5000$ на работе водителя автобуса (/jobs)" },
            { "Обновка", "2 EXP;Купи новый скин на заработанные деньги" },
            { "ПУЛЬТ ОТ ЯДЕРКИ", "1 EXP;Купи в ближайшем магазине пульт от ядерки и используй его" },
            { "Машина мечты", "2 EXP, 3000$;Купи свою первую машину" },
            { "Самый адекватный политик", "3000$;Найди убежище Игоря Гофмана" },
            { "Этот прицел просто имба!", "1500$;Убей игрока с расстояния >1000 метров" },
            { "В поисках Макеевского родничка", "4000$;Где-то на карте есть Макеевский родничок. Тебе нужно найти его, чтобы пройти квест." }
        };
        private List<string> randomServerMessages = new List<string>
        {
            "Во время бомбардировки"
        };
        private Dictionary<int, Vector3> staticPickupInfo = new Dictionary<int, Vector3>
        {
            { 2, new Vector3(-412.52f, -10, 649) },
            { 3, new Vector3(655.63f, -10.75f, 606.9f) },
            //{ 4, new Vector3(656.158f, -10.75f, 605.9f) },
            { 5, new Vector3(413.5f, -9.3f, -561.5f) },
        };
        private Dictionary<int, Vector3> pickupInfo = new Dictionary<int, Vector3>
        {
            { 41, new Vector3(198.1f, 27, -662.8f) },
            { 42, new Vector3(-16.2f, 27, -622.7f) },
            { 43, new Vector3(-279, 27, -583.5f) },
            { 44, new Vector3(-449.8f, 45.9f, -571.5f) },
            { 45, new Vector3(-431.8f, 32.5f, -451.3f) },
            { 46, new Vector3(-372, 27.7f, -264.2f) },
            { 47, new Vector3(-366.9f, 27.7f, -86.3f) },
            { 48, new Vector3(-350.9f, 27.7f, 96.7f) },
            { 49, new Vector3(-324.9f, 27.6f, 260.4f) },
            { 50, new Vector3(-318.7f, 27.6f, 413.1f) },
            { 51, new Vector3(-346.2f, 27.6f, 614.8f) },
            { 52, new Vector3(-166.9f, 26.8f, 725.5f) },
            { 53, new Vector3(39.6f, 25.8f, 708.8f) },
            { 54, new Vector3(142.3f, 25.8f, 444.9f) },
            { 55, new Vector3(318.1f, 51.1f, 165.2f) },
            { 56, new Vector3(394.4f, 32.1f, 360.7f) },
            { 57, new Vector3(526.6f, 27.6f, 390.3f) },
            { 58, new Vector3(663.8f, 27.6f, 458.1f) },
            { 59, new Vector3(693.2f, 27.6f, 438) },
            { 60, new Vector3(716.4f, 25.6f, 287.7f) },
            { 61, new Vector3(690.2f, 44.8f, 138.5f) },
            { 62, new Vector3(582, 23.5f, -41) },
            { 63, new Vector3(569.5f, 26.2f, -234.8f) },
            { 64, new Vector3(583.3f, 27.4f, -414.9f) },
            { 65, new Vector3(744.3f, 25, -562.8f) },
            { 66, new Vector3(751.7f, 25, -663.2f) },
            { 67, new Vector3(690.4f, 26, -725.7f) },
            { 68, new Vector3(594.5f, 25.4f, -783.1f) },
            { 69, new Vector3(421.9f, 24.9f, -751.7f) },
            { 70, new Vector3(295.7f, 28.9f, -702.1f) }
        };
        private List<int> busStopsPickups = new List<int>
        {
            41, 45, 48, 51, 54, 58, 67
        };
        private List<string> helpResponses = new List<string>
        {
            "я - смешная моделька смешного мишки, помогающая игрокам. Нужно что-то еще?",
            "я выдаю квесты игрокам, ведь без квестов на сервере пока нечего делать.",
            "ты можешь выбрать цель на свой вкус - тут можно и стреляться, и обменять здоровье на деньги (работа шахтёра), и спокойно покататься на машинке (работа развозчика), и побомбить захваченные города (работа летчика-бомбардировщика). Выбирай, есть развлечение на любой вкус!",
            "получить повышение уровня можно выполняя квесты. Я могу тебе рассказать о доступных заданиях.",
            "введи /jobs. Там ты найдешь список работ. Учти, что на них можно работать только с определенным уровнем, который ты можешь заработать с помощью квестов.",
            "ты можешь стреляться со случайными игроками за пределами зеленых зон, но учти, что это незаконно.",
            "доната пока нет, о чем я очень сожалею :(",
            "устройся летчиком-бомбардировщиком. Только знай сразу, что бомбить можно будет лишь захваченные инопланетными ящерами города, иначе ты станешь военным преступником.",
            "Игорь Гофман - местная знаменитость. В молодости он был успешным и гениальным предпринимателем, но болезнь сломала его рассудок... Если хочешь навестить его, поищи его квартиру в Куканске. Еще ачивку получишь, да.",
            "Ты опоздал с этим вопросом на 2 года."
        };
        private List<Vector3> bunkerEnterPickups = new List<Vector3>
        {
            new Vector3(631.8f, -10.14f, 530.36f),
            new Vector3(-445.6f, -9.67f, 638.85f),
            new Vector3(245.0f, -10.12f, -316.9f),
            new Vector3(838.9f, -10.18f, -744.99f),
        };
        private List<Vector3> bunkerTeleportPoints = new List<Vector3>
        {
            new Vector3(617.8f, -25.3f, 541.1f),
            new Vector3(634.26f, -9.92f, 530.3f),
            new Vector3(-507f, -13.96f, 670.0f),
            new Vector3(-441.8f, -9.92f, 638.5f),
            new Vector3(281.7f, -13.68f, -271.4f),
            new Vector3(245.1f, -9.92f, -320.5f),
            new Vector3(850.74f, -21f, -733.29f),
            new Vector3(836.82f, -9.9f, -744.9f)
        };
    }
}
