using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Manager
{
    class Manager
    {
        private readonly List<Process> _Processes = new List<Process>();
        public IReadOnlyList<Process> Processes { get { return _Processes; } }

        private readonly ConcurrentPriorityQueue<Process, int> _ProcessesQueue 
            = new ConcurrentPriorityQueue<Process, int>();

        public readonly int quantTimeMS;

        private Task _ProcessorTask;
        public void Run()
        {
            if (_ProcessorTask == null)
            {
                _ProcessorTask = _Processor();
            }
        }

        public void Kill()
        {
            if (_ProcessorTask != null)
            {
                Status = ManagerStatus.aborted;
                _ProcessorTask.Dispose();
                _ProcessorTask = null;
            }
        }

        private bool _exitWhenFree = false;
        public async Task AwaitExit()
        {
            if (_ProcessorTask != null)
            {
                _exitWhenFree = true;
                await _ProcessorTask;
            }
        }

        public ManagerStatus Status { get; private set; }

        public Manager(int quantTimeMS)
        {
            Status = ManagerStatus.aborted;
            this.quantTimeMS = quantTimeMS;
        }


        /// <summary>
        /// Метод для добавления элементов в очередь обработки процессов
        /// </summary>
        /// <param name="process"> - добавляемый процесс</param>
        public async Task<bool> AddProcess(Process process)
        {
            if (_Processes.Find((a) => a.Id == process.Id) != null) return false;

            _Processes.Add(process);
            await _ProcessesQueue.EnqueueAsync(process, process.Priority);
            return true;
        }


        /// <summary>
        /// Удаляет из списка процессов те процессы, что уже завершили работу
        /// </summary>
        /// <returns>
        /// <list type="bullet">
        /// true - если был удалён хоть один элемент, 
        /// </list>
        /// <list type="bullet">
        /// false - если не было удалено ни одного элемента
        /// </list>
        /// </returns>
        public bool ClearDeadProcesses()
        {
            bool isDelete = false;

            foreach (var elem in _Processes)
            {
                if (!elem.IsAlive)
                {
                    isDelete = true;
                    _Processes.Remove(elem);
                }
            }

            return isDelete;
        }

        private async Task _Processor()
        {
            Status = ManagerStatus.busy;

            while (Status != ManagerStatus.aborted)
            {
                if (Status != ManagerStatus.aborted)
                {
                    if (await _ProcessesQueue.CountAsync() == 0)
                    {
                        Status = ManagerStatus.free;
                        if (_exitWhenFree == true) break;

                        await Task.Delay(quantTimeMS);
                    }
                    else
                    {
                        Status = ManagerStatus.busy;
                        var process = await _ProcessesQueue.DequeueAsync();
                        Console.WriteLine($"Обработка процесса {process.Name}");
                        process.Size -= 1;

                        await Task.Delay(quantTimeMS);
                        if (process.IsAlive)
                            await _ProcessesQueue.EnqueueAsync(process, process.Priority);
                    }

                }
            }
        }
    }

    public class ReverserComparer<TKey> : IComparer<TKey> where TKey : struct
    {
        // Call CaseInsensitiveComparer.Compare with the parameters reversed.
        int IComparer<TKey>.Compare(TKey x, TKey y)
        {
            return ((new CaseInsensitiveComparer()).Compare(y, x));
        }
    }

    enum ManagerStatus
    {
        free,
        busy,
        aborted
    }

    public class Process
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Priority { get; set; }
        public int InitialSize { get; set; }
        public int Size { get; set; }
        public bool IsAlive { get => Size != 0; }
        public double CompletePercentage { get => 100.0 - (Size * 100.0 / InitialSize ); }

        //public Process(int id, string name, int priority, int initialSize)
        //{
        //    Id = id;
        //    Name = name;
        //    Priority = priority;
        //    Size = InitialSize = initialSize;
        //}
    }

    class ConcurrentPriorityQueue<TElement, TKey>
        where TElement : class
        where TKey : IComparable<TKey>
    {
        private readonly PriorityQueue<TElement, TKey> _Queue = new PriorityQueue<TElement, TKey>();
        private bool _queueIsFree = true;

        public async Task EnqueueAsync(TElement process, TKey priority)
        {
            await Task.Factory.StartNew(() =>
            {
                while (!_queueIsFree) ;
            });

            _queueIsFree = false;
            _Queue.Enqueue(process, priority);
            _queueIsFree = true;
        }

        public async Task<TElement> DequeueAsync()
        {
            await Task.Factory.StartNew(() =>
            {
                while (!_queueIsFree) ;
            });

            _queueIsFree = false;
            if (_Queue.Count == 0) return null;
            var process = _Queue.Dequeue();
            _queueIsFree = true;

            return process;
        }

        public async Task<int> CountAsync()
        {
            await Task.Factory.StartNew(() =>
            {
                while (!_queueIsFree) ;
            });

            _queueIsFree = false;
            var size = _Queue.Count;
            _queueIsFree = true;

            return size;
        }

        public async Task<List<TElement>> GetElementsAsync()
        {
            await Task.Factory.StartNew(() =>
            {
                while (!_queueIsFree) ;
            });

            _queueIsFree = false;
            var list = _Queue.GetList();
            _queueIsFree = true;

            return list;
        }
    }

    class PriorityQueue<TElement, TKey>
        where TElement : class
        where TKey : IComparable<TKey>
    {
        private class Elem
        {
            public TElement element;
            public TKey key;
            public Elem next = null;
            public Elem prev = null;
        }
        public int Count { get; private set; }
        private readonly Elem _Tale = new Elem();
        private Elem _Last = null;

        public PriorityQueue() { }

        public void Enqueue(TElement element, TKey key)
        {
            var prev = _Tale;
            while (prev.next != null && prev.next.key.CompareTo(key) < 0)
            {
                prev = prev.next;
            }
            var elem = new Elem()
            {
                element = element,
                key = key,
                next = prev.next,
                prev = prev
            };
            prev.next = elem;
            if (elem.next != null) elem.next.prev = elem;
            if (elem.next == null) _Last = elem;

            Count++;
        }

        public TElement Dequeue()
        {
            if (_Last == null)
            {
                return null;
            }

            var elem = _Last;
            _Last = elem.prev == _Tale ? null : elem.prev;
            Count--;
            return elem.element;
        }

        public List<TElement> GetList()
        {
            var list = new List<TElement>();
            var elem = _Tale.next;
            while (elem != null)
            {
                list.Add(elem.element);
                elem = elem.next;
            }
            return list;
        }
    }
}
