using Errors;
using Lexing;
using Parsing;
using Interpreter;

string file = "main.vptr";

if(args.Length > 0)
{
    if(File.Exists(args[0]))
    {
        file = args[0];
    }
    else
    {
        ErrorHandler.Throw($"Input file '{args[0]}' was not found.");
    }
}
else
{
    if(!File.Exists(file))
    {
        ErrorHandler.Throw("No 'main.vptr' file could be found. Either create one or pass your file.");
    }
}

// --- Generating Tokens ---
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

// --- Generating Instructions ---
Console.WriteLine("-Parsing");

Parser parser = new(tokens);

List<Instruction> instructions = parser.MakeInstructs();

string instStr = "";
int instrCount = 0;
foreach(Instruction inst in instructions)
{
    instStr += instrCount +": "+ inst.ToString() + "\n";
    instrCount++;
}
File.WriteAllText("inst_dump.txt",instStr);

// --- Running code ---
Console.WriteLine("Executing.");

Engine intpr = new(instructions);
intpr.Run();

