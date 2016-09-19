using System.Threading;

namespace Brimstone
{
	internal interface IReferenceCount
	{
		void Increment();
		void Decrement();
		long Count { get; }
	}

	internal class ReferenceCount : IReferenceCount
	{
		private long _count;

		public ReferenceCount() {
			_count = 1;
		}

		public void Increment() {
			++_count;
		}

		public void Decrement() {
			--_count;
		}

		public long Count {
			get { return _count; }
		}
	}

	internal class ReferenceCountInterlocked : IReferenceCount
	{
		private long _count;

		public ReferenceCountInterlocked() {
			_count = 1;
		}

		public void Increment() {
			Interlocked.Increment(ref _count);
		}

		public void Decrement() {
			Interlocked.Decrement(ref _count);
		}

		public long Count {
			get { return Interlocked.Read(ref _count); }
		}
	}
}
