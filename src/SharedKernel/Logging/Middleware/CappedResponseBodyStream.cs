using System.Buffers;

namespace SharedKernel.Logging.Middleware;

internal sealed class CappedResponseBodyStream : Stream
{
   private readonly Stream _inner;
   private readonly int _capBytes;
   private readonly byte[] _buf;
   private int _bufLen;
   private long _totalWritten;

   public CappedResponseBodyStream(Stream inner, int capBytes)
   {
      _inner = inner;
      _capBytes = capBytes;
      _buf = ArrayPool<byte>.Shared.Rent(_capBytes);
      _bufLen = 0;
      _totalWritten = 0;
   }

   public long TotalWritten
   {
      get { return _totalWritten; }
   }

   public ReadOnlyMemory<byte> Captured
   {
      get { return new ReadOnlyMemory<byte>(_buf, 0, _bufLen); }
   }

   private void Capture(ReadOnlySpan<byte> src)
   {
      if (_bufLen >= _capBytes)
      {
         return;
      }

      var toCopy = Math.Min(_capBytes - _bufLen, src.Length);
      if (toCopy <= 0)
      {
         return;
      }

      src[..toCopy]
         .CopyTo(_buf.AsSpan(_bufLen));
      _bufLen += toCopy;
   }

   public override void Write(byte[] buffer, int offset, int count)
   {
      _inner.Write(buffer, offset, count);
      _totalWritten += count;
      Capture(new ReadOnlySpan<byte>(buffer, offset, count));
   }

   public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
   {
      await _inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
      _totalWritten += count;
      Capture(new ReadOnlySpan<byte>(buffer, offset, count));
   }

   public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
   {
      var vt = _inner.WriteAsync(buffer, cancellationToken);
      _totalWritten += buffer.Length;
      Capture(buffer.Span);
      return vt;
   }

   public override void Flush()
   {
      _inner.Flush();
   }

   public override Task FlushAsync(CancellationToken cancellationToken)
   {
      return _inner.FlushAsync(cancellationToken);
   }

   protected override void Dispose(bool disposing)
   {
      try
      {
         base.Dispose(disposing);
      }
      finally
      {
         ArrayPool<byte>.Shared.Return(_buf);
      }
   }

   public override bool CanRead
   {
      get { return false; }
   }

   public override bool CanSeek
   {
      get { return false; }
   }

   public override bool CanWrite
   {
      get { return true; }
   }

   public override long Length
   {
      get { throw new NotSupportedException(); }
   }

   public override long Position
   {
      get { throw new NotSupportedException(); }
      set { throw new NotSupportedException(); }
   }

   public override int Read(byte[] buffer, int offset, int count)
   {
      throw new NotSupportedException();
   }

   public override long Seek(long offset, SeekOrigin origin)
   {
      throw new NotSupportedException();
   }

   public override void SetLength(long value)
   {
      throw new NotSupportedException();
   }
}