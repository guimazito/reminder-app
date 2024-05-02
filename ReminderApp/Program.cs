using System.Text.Json;
using System.Text.RegularExpressions;

namespace ReminderApp
{

    static class Globals
    {
        public static string appname = "ReminderApp.exe";
        public static string filename = "myReminders.json";
    }

    public struct Reminder
    {
        public Reminder(long id, string title, string status, long dueDate)
        {
            Id = id;
            Title = title;
            Status = status;
            DueDate = dueDate;
        }
        public long Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public long DueDate { get; set; }

        public override string ToString() => $"{Id}.{Title}.{Status}.{DueDate}";
    }

    public class Program
    {
        public static void Main(string[] args)
        {

            List<Reminder> list;
            bool foundStatus = false;

            try
            {
                switch (args[0])
                {
                    case "--add":
                        string? title = null;
                        string? dueDate = null;

                        Console.WriteLine("Adding new reminder...\n" +
                            "ps: Fields cannot be blank!\n");

                        while (String.IsNullOrEmpty(title))
                        {
                            Console.Write("Title: ");
                            title = Console.ReadLine();
                        }

                        do
                        {
                            Console.Write("Due Date (dd/mm/yyyy hh:mm): ");
                            dueDate = Console.ReadLine();
                        } while (!IsValidDate(dueDate));

                        var newReminder = new Reminder(GenerateNewId(), title, "Pending", DatetimeToTimestamp(dueDate));

                        try
                        {
                            // If file exists add new reminder
                            list = GetDataFile();
                            list.Add(newReminder);
                            SaveDataFile(list);
                        }
                        catch (FileNotFoundException)
                        {
                            // Creating file and saving first reminder
                            List<Reminder> newReminderList = new();
                            newReminderList.Add(newReminder);
                            SaveDataFile(newReminderList);
                        }

                        Console.WriteLine("\nReminder created!");

                        break;

                    case "--delete":
                        IsFileCreated();
                        if (args[1] != null)
                        {
                            
                            list = GetDataFile();                            

                            foreach (var i in list)
                            {
                                if (args[1] == i.Id.ToString())
                                {
                                    list.Remove(i);
                                    PrintItem(i.Id, i.Title, i.Status, i.DueDate);
                                    Console.WriteLine("Reminder deleted!");
                                    SaveDataFile(list);
                                    foundStatus = true;
                                    break;
                                }
                            }
                            WasReminderFound(foundStatus);
                        }

                        break;

                    case "--list-all":
                        IsFileCreated();
                        UpdateStatus();

                        list = GetDataFile();

                        foreach (var i in list)
                        {
                            PrintItem(i.Id, i.Title, i.Status, i.DueDate);
                            foundStatus = true;
                        }

                        WasReminderFound(foundStatus);

                        break;

                    case "--list-pending":
                        IsFileCreated();
                        UpdateStatus();

                        list = GetDataFile();
                        var pendingItems = list.Where(i => i.Status == "Pending");

                        foreach (var i in pendingItems)
                        {
                            PrintItem(i.Id, i.Title, i.Status, i.DueDate);
                            foundStatus = true;
                        }

                        WasReminderFound(foundStatus);

                        break;

                    case "--list-completed":
                        IsFileCreated();
                        UpdateStatus();
                        
                        list = GetDataFile();
                        var completedItems = list.Where(i => i.Status == "Completed");

                        foreach (var i in completedItems)
                        {
                            PrintItem(i.Id, i.Title, i.Status, i.DueDate);
                            foundStatus = true;
                        }

                        WasReminderFound(foundStatus);

                        break;

                    case "--mark-as-done":
                        IsFileCreated();
                        if (args[1] != null)
                        {
                            list = GetDataFile();

                            for (int i = 0; i < list.Count; i++)
                            {
                                if (args[1] == list[i].Id.ToString())
                                {
                                    var s = list[i];
                                    s.Status = "Completed";
                                    list[i] = s;
                                    SaveDataFile(list);
                                    PrintItem(list[i].Id, list[i].Title, list[i].Status, list[i].DueDate);
                                    Console.WriteLine("Reminder updated!");
                                    foundStatus = true;
                                    break;
                                }
                            }
                            WasReminderFound(foundStatus);
                        }

                        break;

                    case "--help":
                        HelpList();
                        break;

                    default:
                        Console.WriteLine($"Error: unrecognized or incomplete command line!");
                        HelpList();
                        break;

                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine($"Error: unrecognized or incomplete command line!");
                HelpList();
            }
        }

        public static void WasReminderFound(bool status)
        {
            // Checking if reminder was found during latest search
            if (!status)
            {
                Console.WriteLine("No reminder found!");
            }
        }

        public static void PrintItem(long id, string title, string status, long duedate)
        {
            Console.WriteLine($"Id: {id}");
            Console.WriteLine($"Title: {title}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"Due Date: {TimestampToDatetime(duedate)}\n");
        }

        public static void SaveDataFile(List<Reminder> list)
        {
            // Saving all reminders in file
            string jsonString = JsonSerializer.Serialize(list);
            File.WriteAllText(Globals.filename, jsonString);
        }

        public static List<Reminder> GetDataFile()
        {
            // Getting all reminders from file
            string file = File.ReadAllText(Globals.filename);
            var list = JsonSerializer.Deserialize<List<Reminder>>(file);

            return list;
        }
        
        public static void UpdateStatus()
        {
            /* Checking if all pending reminders due date
             * has already been exceeded. In positive case,
             * the status is changed to completed */  
            var list = GetDataFile();
            DateTime parsedDate;
            DateTime currentDateTime = DateTime.Now;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Status == "Pending")
                {
                    parsedDate = DateTime.Parse(TimestampToDatetime(list[i].DueDate));
                    var diff = currentDateTime - parsedDate;
                    //Console.WriteLine(diff.Milliseconds);

                    if (diff.Milliseconds > 0)
                    {
                        var s = list[i];
                        s.Status = "Completed";
                        list[i] = s;
                        SaveDataFile(list);
                    }
                }
            }
        }

        public static void IsFileCreated()
        {
            // Checking if json file exists before any action
            if (!File.Exists(Globals.filename))
            {
                Console.WriteLine("There's no reminder created!");
                Environment.Exit(0);
            }
        }

        public static bool IsValidDate(string date)
        {
            /* First of all, a regex is used in due date mask to limit user input.
             * Second, the due date inserted is checked if exists in calendar.
             * Finally, the due date inserted is checked if it's not a datetime 
             * in the past */
            DateTime tempDate;
            var pattern = @"^[0-9]{2}/[0-9]{2}/[0-9]{4}\s[0-9]{2}:[0-9]{2}$";

            if (Regex.IsMatch(date, pattern))
            {
                if (DateTime.TryParse(date, out tempDate))
                {
                    DateTime currentDateTime = DateTime.Now;
                    var diff = currentDateTime - tempDate;
                    // Console.WriteLine(diff.Milliseconds);

                    if (diff.Milliseconds < 0)
                    {
                        return true;

                    }

                }
            }

            return false;
        }

        public static long DatetimeToTimestamp(string dueDate)
        {
            // Converting string to datetime
            var dt = DateTime.Parse(dueDate);
            // Getting offset from due date utc time
            DateTimeOffset dto = new(dt);
            // Getting unix timestamp in seconds
            long unixTime = dto.ToUnixTimeSeconds();
            return unixTime;
        }

        public static string TimestampToDatetime(long dueDate)
        {
            // Converting timestamp duedate to string specific format
            var offset = DateTimeOffset.FromUnixTimeSeconds(dueDate);
            return offset.LocalDateTime.ToString("dd/MM/yyyy HH:mm");
        }

        public static void HelpList()
        {
            // Help list menu
            Console.WriteLine("\nUSAGE:");
            Console.WriteLine($"    {Globals.appname} --help                        print this help message");
            Console.WriteLine($"    {Globals.appname} --add                         create a new reminder");
            Console.WriteLine($"    {Globals.appname} --delete <id>                 delete a specific reminder");
            Console.WriteLine($"    {Globals.appname} --list-all                    list all reminders");
            Console.WriteLine($"    {Globals.appname} --list-pending                list all pending reminders");
            Console.WriteLine($"    {Globals.appname} --list-completed              list all completed reminders");
            Console.WriteLine($"    {Globals.appname} --mark-as-done <id>           change a specific reminder to completed");
        }

        public List<string> CollectIdList()
        {
            // Getting all reminders id created
            var list = GetDataFile();
            List<string> idList = new();

            foreach (var i in list)
            {
                idList.Add($"{i.Id}");
            }

            // idList.ForEach(Console.WriteLine);
            return idList;
        }

        public static long GenerateNewId()
        {
            // Checking if id already exists before generate one
            long id;
            var rand = new Random();
            try
            {
                Program p = new();
                List<string> idList = p.CollectIdList();
                id = rand.NextInt64(1, 10000);

                while (idList.Contains(id.ToString()))
                {
                    id = rand.NextInt64(1, 10000);
                }
            }
            catch (FileNotFoundException)
            {
                id = rand.NextInt64(1, 10000);
            }
            //Console.WriteLine($"Id: {id}");

            return id;
        }
    }
}