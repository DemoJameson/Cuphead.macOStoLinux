using System.IO;
using System.Text;

namespace Cuphead.macOStoLinux; 

public class LogWriter : TextWriter {

    public TextWriter STDOUT;
    public TextWriter File;

    public override Encoding Encoding {
        get {
            return STDOUT?.Encoding ?? File?.Encoding;
        }
    }

    public override void Write(string value) {
        STDOUT?.Write(value);
        File?.Write(value);
        File?.Flush();
    }

    public override void WriteLine(string value) {
        STDOUT?.WriteLine(value);
        File?.WriteLine(value);
        File?.Flush();
    }

    public override void Write(char value) {
        STDOUT?.Write(value);
        File?.Write(value);
        File?.Flush();
    }

    public override void Write(char[] buffer, int index, int count) {
        STDOUT?.Write(buffer, index, count);
        File?.Write(buffer, index, count);
        File?.Flush();
    }

    public override void Flush() {
        STDOUT?.Flush();
        File?.Flush();
    }

    public override void Close() {
        STDOUT?.Close();
        STDOUT = null;
        File?.Close();
        File = null;
    }

}