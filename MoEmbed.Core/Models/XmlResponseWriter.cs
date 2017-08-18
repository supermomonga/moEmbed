using System;
using System.IO;
using System.Text;
using System.Xml;

namespace MoEmbed.Models
{
    public class XmlResponseWriter : IResponseWriter
    {
        private readonly bool _LeaveOpen;

        public XmlResponseWriter(Stream stream, bool leaveOpen = false)
        {
            BaseWriter = XmlWriter.Create(new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen));
        }

        public XmlResponseWriter(TextWriter textWriter, bool leaveOpen = false)
        {
            BaseWriter = XmlWriter.Create(textWriter, new XmlWriterSettings()
            {
                CloseOutput = !leaveOpen
            });
        }

        public XmlResponseWriter(XmlWriter baseWriter, bool leaveOpen = false)
        {
            BaseWriter = baseWriter;
            _LeaveOpen = leaveOpen;
        }
        protected XmlWriter BaseWriter { get; private set; }

        public void WriteStartResponse(string name)
        {
            ThrowIfDisposed();
            BaseWriter.WriteStartElement(name);
        }

        public void WriteProperty(string name, bool value)
            => WriteProperty(name, value ? "true" : "false");

        public void WriteProperty(string name, double value)
            => WriteProperty(name, value.ToString("r"));

        public void WriteProperty(string name, object value)
        {
            ThrowIfDisposed();
            BaseWriter.WriteStartElement(name);
            BaseWriter.WriteString(value?.ToString() ?? string.Empty);
            BaseWriter.WriteEndElement();
        }

        public void WriteEndResponse()
        {
            ThrowIfDisposed();
            BaseWriter.WriteEndElement();
            BaseWriter.Flush();
        }

        public void WriteStartArrayProperty(string name)
        {
            ThrowIfDisposed();
            BaseWriter.WriteStartElement(name);
        }

        public void WriteEndArrayProperty()
        {
            ThrowIfDisposed();
            BaseWriter.WriteEndElement();
        }

        public void WriteStartObjectProperty(string name)
        {
            ThrowIfDisposed();
            BaseWriter.WriteStartElement(name);
        }

        public void WriteEndObjectProperty()
        {
            ThrowIfDisposed();
            BaseWriter.WriteEndElement();
        }

        public void WriteArrayValue(string name, bool value)
            => WriteArrayValue(name, value ? "true" : "false");

        public void WriteArrayValue(string name, double value)
            => WriteArrayValue(name, value.ToString("r"));

        public void WriteArrayValue(string name, object value)
        {
            ThrowIfDisposed();
            BaseWriter.WriteStartElement(name);
            BaseWriter.WriteString(value?.ToString() ?? string.Empty);
            BaseWriter.WriteEndElement();
        }

        private void ThrowIfDisposed()
        {
            if (BaseWriter == null)
            {
                throw new ObjectDisposedException(nameof(BaseWriter));
            }
        }

        public void Dispose()
        {
            if (!_LeaveOpen)
            {
                BaseWriter?.Dispose();
            }
            BaseWriter = null;
        }
    }
}
