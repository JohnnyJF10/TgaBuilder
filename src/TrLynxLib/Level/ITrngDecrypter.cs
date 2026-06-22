namespace TrLynxLib.Level
{
    public interface ITrngDecrypter
    {
        bool DecryptLevel(string source, string target);
    }
}