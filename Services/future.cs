using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VideoTools.Services
{
    public class PromiseSharedValue<T>
    {
        public PromiseSharedValue(SemaphoreSlim semaphore)
        {
            this.semaphore = semaphore;
        }
        public SemaphoreSlim semaphore;
        public T? value;
    }
    public class Future<T>
    {
        PromiseSharedValue<T> value;
        public Future(PromiseSharedValue<T> value)
        {
            this.value = value;
        }
        public T? get()
        {
            value.semaphore.Wait();
            return value.value;
        }
        public void wait()
        {
            value.semaphore.Wait();
        }
    }
    public class Promise<T>
    {
        PromiseSharedValue<T> value;
        SemaphoreSlim semaphore = new SemaphoreSlim(0);
        int futures = 0;
        public Promise()
        {
            value = new PromiseSharedValue<T>(semaphore);
        }
        public void set_value(T value)
        {
            this.value.value = value;
            this.value.semaphore.Release(futures);
        }
        public Future<T> get_future()
        {
            var f = new Future<T>(value);
            futures++;
            return f;
        }
    }

}
