using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace MoEmbed.Models
{
    public class JsonResponseWriter : IResponseWriter
    {
        private readonly bool _LeaveOpen;

        public JsonResponseWriter(Stream stream, bool leaveOpen = false)
        {
            BaseWriter = new JsonTextWriter(new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen));
        }

        public JsonResponseWriter(TextWriter textWriter, bool leaveOpen = false)
        {
            BaseWriter = new JsonTextWriter(textWriter)
                {
                    CloseOutput = !leaveOpen
                };
        }

        public JsonResponseWriter(JsonWriter baseWriter, bool leaveOpen = false)
        {
            BaseWriter = baseWriter;
            _LeaveOpen = leaveOpen;
        }

        protected JsonWriter BaseWriter { get; private set; }

        public void WriteStartResponse(string name)
        {
            ThrowIfDisposed();
            BaseWriter.WriteStartObject();
        }

        public void WriteProperty(string name, bool value)
        {
            ThrowIfDisposed();
            BaseWriter.WritePropertyName(name);
            BaseWriter.WriteValue(value);
        }

        public void WriteProperty(string name, double value)
        {
            ThrowIfDisposed();
            BaseWriter.WritePropertyName(name);
            BaseWriter.WriteValue(value);
        }

        public void WriteProperty(string name, object value)
        {
            ThrowIfDisposed();
            BaseWriter.WritePropertyName(name);
            BaseWriter.WriteValue(value);
        }

        public void WriteEndResponse()
        {
            ThrowIfDisposed();
            BaseWriter.WriteEndObject();
            BaseWriter.Flush();
        }

        public void WriteStartArrayProperty(string name)
        {
            ThrowIfDisposed();
            BaseWriter.WritePropertyName(name);
            BaseWriter.WriteStartArray();
        }

        public void WriteEndArrayProperty()
        {
            ThrowIfDisposed();
            BaseWriter.WriteEndArray();
        }

        public void WriteStartObjectProperty(string name)
        {
            ThrowIfDisposed();
            BaseWriter.WritePropertyName(name);
            BaseWriter.WriteStartObject();
        }

        public void WriteEndObjectProperty()
        {
            ThrowIfDisposed();
            BaseWriter.WriteEndObject();
        }

        public void WriteStartObject(string name)
        {
            ThrowIfDisposed();
            BaseWriter.WriteStartObject();
        }

        public void WriteEndObject()
        {
            ThrowIfDisposed();
            BaseWriter.WriteEndObject();
        }

        public void WriteArrayValue(string name, bool value)
        {
            ThrowIfDisposed();
            BaseWriter.WriteValue(value);
        }

        public void WriteArrayValue(string name, double value)
        {
            ThrowIfDisposed();
            BaseWriter.WriteValue(value);
        }

        public void WriteArrayValue(string name, object value)
        {
            ThrowIfDisposed();
            BaseWriter.WriteValue(value);
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
                BaseWriter?.Close();
            }
            BaseWriter = null;
        }
    }
}
