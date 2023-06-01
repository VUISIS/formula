using System;
using System.IO;
using Antlr4.Runtime;

namespace Core.API.Parser
{
    class FormulaLexerErrorListener : BaseErrorListener
    {
        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {

            Console.WriteLine("Error at line {0} column {1}: found token {2}. Expected: ", line, charPositionInLine, offendingSymbol.Text);

            foreach (var item in e.GetExpectedTokens().ToList())
            {
                Console.Write(FormulaLexer.DefaultVocabulary. GetSymbolicName(item) + ", ");
            }

            throw new Exception("Syntax error in 4ml file.");
        }
    }
}
