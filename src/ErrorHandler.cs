using Lexing;

namespace Errors;

public static class ErrorHandler
{
    public static void Throw(string msg)
    {
        Console.Error.WriteLine("Error: " + msg);
        Environment.Exit(-1);
    }

    public static void Throw(string msg, int line)
    {
        Throw(msg + $" at: line {line}");
    }

    public static void Throw(string msg, Token faulty)
    {
        Throw(msg + $" at: line {faulty.line} in {faulty.file}");
    }
}