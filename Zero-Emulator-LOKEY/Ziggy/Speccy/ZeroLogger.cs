using System;
using System.IO;

namespace Loggers
{
    public class ZeroLogger: IDisposable
    {
        StreamWriter sw;
        String filePath;
		public ZeroLogger(string fpath)
		{
			if (string.IsNullOrWhiteSpace(fpath))
			{
				throw new ArgumentNullException("logger was empty");
			}
			else
			{
				filePath = fpath;
			}
		}
        public void Log(string s, bool finalise = false)
        {
			if (sw == null)
			{
				sw = new StreamWriter(filePath);
			}

            sw.WriteLine(s);
            sw.Flush();

            if (finalise)
                sw.Close();
        }

        public void DebugLog(string s)
        {
            System.Diagnostics.Debug.WriteLine(s);
        }

        public void Dispose()
        {
            if(sw != null)
            {
                sw.Close();
                File.Delete(filePath);
            }
        }
    }
}
