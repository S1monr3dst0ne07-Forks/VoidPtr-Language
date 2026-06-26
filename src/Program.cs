using Errors;
using Interpreter;
using Lexing;
using Parsing;

string file = "D:/Personal/ProgramingProjects/Csharp_Projects/ByteLang/src/main.vptr";


if(args.Length > 0)
{
    if(File.Exists(args[0]))
    {
        file = args[0];
    }
    else
    {
        ErrorHandler.Throw($"Input file '{file}' was not found.");
    }
}
else
{
    if(!File.Exists(file))
    {
        ErrorHandler.Throw("No 'main.vptr' file could be found. Either create one or pass your file.");
    }
}

Lexer l = new(File.ReadAllText(file),file);

Console.WriteLine("-Lexing");

string toks = "";
List<Token> tokens = l.MakeTokens();
PreProcessor processor = new(tokens);
tokens = processor.Process();

foreach(Token t in tokens)
{
    toks+=t.ToString() + "\n";
}


File.WriteAllText("tok_dump.txt",toks);

Console.WriteLine("-Parsing");
Parser parser = new(tokens);

List<Instruction> instructions = parser.MakeInstructs();



string instStr = "";
foreach(Instruction inst in instructions)
{
    instStr += inst.ToString() + "\n";
}

File.WriteAllText("inst_dump.txt",instStr);

Console.WriteLine("Executing.");

Engine intpr = new(instructions);
intpr.Run();

