/*
	Copyright 2016, 2017 Katy Coe

	This file is part of Brimstone.

	Brimstone is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Brimstone is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with Brimstone.  If not, see <http://www.gnu.org/licenses/>.
*/

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
