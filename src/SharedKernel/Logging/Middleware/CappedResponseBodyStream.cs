using System.Buffers;

namespace SharedKernel.Logging.Middleware;

internal sealed class CappedResponseBodyStream(Stream inner, int capBytes) : Stream
{
   private readonly byte[] _buffer = ArrayPool<byte>.Shared.Rent(capBytes);
   private int _bufferLength;
   private bool _disposed;
   public long TotalWritten { get; private set; }

   public ReadOnlyMemory<byte> Captured => new(_buffer, 0, _bufferLength);

   public override bool CanRead => false;
   public override bool CanSeek => false;
   public override bool CanWrite => true;
   public override long Length => throw new NotSupportedException();

   public override long Position
   {
      get => throw new NotSupportedException();
      set => throw new NotSupportedException();
   }

   private void Capture(ReadOnlySpan<byte> source)
   {
      if (_bufferLength >= capBytes)
         return;

      var toCopy = Math.Min(capBytes - _bufferLength, source.Length);
      if (toCopy <= 0)
         return;

      source[..toCopy]
         .CopyTo(_buffer.AsSpan(_bufferLength));
      _bufferLength += toCopy;
   }

   public override void Write(byte[] buffer, int offset, int count)
   {
      inner.Write(buffer, offset, count);
      TotalWritten += count;
      Capture(buffer.AsSpan(offset, count));
   }

   public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
   {
      await inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
      TotalWritten += count;
      Capture(buffer.AsSpan(offset, count));
   }

   public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
   {
      var task = inner.WriteAsync(buffer, cancellationToken);
      TotalWritten += buffer.Length;
      Capture(buffer.Span);
      return task;
   }

   public override void Flush() => inner.Flush();

   public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

   protected override void Dispose(bool disposing)
   {
      if (_disposed)
         return;

      _disposed = true;
      ArrayPool<byte>.Shared.Return(_buffer);
      base.Dispose(disposing);
   }

   public override ValueTask DisposeAsync()
   {
      if (_disposed)
         return ValueTask.CompletedTask;

      _disposed = true;
      ArrayPool<byte>.Shared.Return(_buffer);
      return ValueTask.CompletedTask;
   }

   public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
   public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
   public override void SetLength(long value) => throw new NotSupportedException();
}