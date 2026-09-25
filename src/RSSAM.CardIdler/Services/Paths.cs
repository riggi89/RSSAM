namespace RSSAM.CardIdler.Services;

public static class Paths
{
    public static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RSSAM", "CardIdler");

    public static string File(string name)
    {
        Directory.CreateDirectory(Dir);
        return Path.Combine(Dir, name);
    }
}
