using System.Globalization;
#pragma warning disable CA1806

namespace TVProfilSched;

static class Program
{
    public static IEnumerable<DateTime> DateRange(DateTime from, DateTime to)
    {
        for (var day = from.Date; day.Date <= to.Date; day = day.AddDays(1))
            yield return day;
    }

    public static void Main()
    {
        Console.WriteLine("Type a starting date (yyyy-MM-dd): ");
        string startDate = Console.ReadLine();
        Console.WriteLine("Type a ending date: ");
        string endDate = Console.ReadLine();
        endDate = !string.IsNullOrWhiteSpace(endDate) ? endDate : DateTime.Now.ToString("yyyy-MM-dd");
        Console.WriteLine("Type a channel name: ");
        string channel = Console.ReadLine();
        Console.WriteLine("Type a search term: ");
        string searchTerm = Console.ReadLine();

        if (File.Exists("out.txt")) File.Delete("out.txt");
        using FileStream fileStream = File.Open("out.txt", FileMode.Append);
        using StreamWriter file = new(fileStream);
        file.AutoFlush = true;

        if (!(DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start)
            && DateTime.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end)))
        {
            Console.WriteLine("Invalid starting date!");
            return;
        }

        new PausableForEach(DateRange(start, end).ToList(), file, channel, searchTerm);

        /*
        string[] contents = File.ReadAllLines("out.txt");
        Array.Sort(contents);
        File.WriteAllLines("out.txt", contents);
        */
    }
}