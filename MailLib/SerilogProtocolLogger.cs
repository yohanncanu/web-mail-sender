// https://github.com/jstedfast/MailKit/issues/761
// ProtocolLogger.cs
using MailKit;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

namespace MailLib
{

    internal class SerilogProtocolLogger : IProtocolLogger
    {
        private static readonly string _clientPrefix = "C: ";
        private static readonly string _serverPrefix = "S: ";

        private readonly ILogger<SerilogProtocolLogger> _logger;

        public IAuthenticationSecretDetector AuthenticationSecretDetector { get; set; }

        public SerilogProtocolLogger(ILogger<SerilogProtocolLogger> logger)
        {
            _logger = logger;
        }

        public void LogConnect(Uri uri) =>
          _logger.LogInformation("Connected to {URI}", uri);

        public void LogClient(byte[] buffer, int offset, int count)
        {
            ValidateArguments(buffer, offset, count);

            Log(_clientPrefix, buffer, offset, count);
        }

        public void LogServer(byte[] buffer, int offset, int count)
        {
            ValidateArguments(buffer, offset, count);

            Log(_serverPrefix, buffer, offset, count);
        }


        public void Dispose() { }

        private static void ValidateArguments(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
        }

        private void Log(string prefix, byte[] buffer, int offset, int count)
        {
            int endIndex = offset + count;
            int index = offset;
            int start;

            while (index < endIndex)
            {
                start = index;

                while (index < endIndex && buffer[index] != (byte)'\n')
                {
                    index++;
                }

                if (index < endIndex && buffer[index] == (byte)'\n')
                {
                    index++;
                }
                var val = Encoding.Default.GetString(buffer, start, index - start).Trim();
                _logger.LogInformation(prefix + val);
            }
        }


    }
}