using System;
using System.Text;
using System.IO;

using Microsoft.Formula.CommandLine;
using Microsoft.Formula.API;

namespace Debugger;

public class DebuggerSink : IMessageSink
{
    private StringBuilder strBuilder;

    public string Output {
        get 
        {
            return strBuilder.ToString();
        }
    }

    public DebuggerSink() 
    {
        strBuilder = new StringBuilder();
    }

    public void ClearOutput()
    {
        strBuilder.Clear();
    }

    private void AddMessage(SeverityKind severity = SeverityKind.Info, string msg = "", bool newline = false)
    {
        if (msg.Contains("[]>"))
        {
            strBuilder.Append("\n");
        }
        
        if(newline)
        {
            strBuilder.AppendLine(msg);
            return;
        }
        strBuilder.Append(msg);
    }

    public TextWriter Writer
    {
        get { return TextWriter.Null; }
    }

    public void WriteMessage(string msg)
    {
        AddMessage(SeverityKind.Info, msg);
    }

    public void WriteMessage(string msg, SeverityKind severity)
    {
        AddMessage(severity, msg);
    }

    public void WriteMessageLine(string msg)
    {
        AddMessage(SeverityKind.Info, msg, true);
    }

    public void WriteMessageLine(string msg, SeverityKind severity)
    {
        AddMessage(severity, msg, true);
    } 

    public void ResetPrintedError()
    {
    }
}