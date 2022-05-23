using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace N_Manager
{
    public class Manager
    {
        public List<Func<Task<int>>> events = new List<Func<Task<int>>>();
        public readonly List<Process> Processes = new List<Process>();

        public readonly ConcurrentPriorityQueue<Process, int> ProcessesQueue
            = new ConcurrentPriorityQueue<Process, int>();

        public int quantTimeMS;

        private Task _ProcessorTask;
        public void Run()
        {
            if (_ProcessorTask == null)
            {
                Status = ManagerStatus.busy;
                _ProcessorTask = _Processor();
            }
        }

        public async Task AwaitExit()
        {
            if (_ProcessorTask != null)
            {
                Status = ManagerStatus.aborted;
                await _ProcessorTask;
                _ProcessorTask = null;
            }
        }

        public async Task Reset()
        {
            var exit = AwaitExit();
            Processes.Clear();
            await exit;
            await ProcessesQueue.ResetAsync();
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
            if (Processes.Find((a) => a.Id == process.Id) != null) return false;

            Processes.Add(process);
            await ProcessesQueue.EnqueueAsync(process, process.Priority);
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

            foreach (var elem in Processes)
            {
                if (!elem.IsAlive)
                {
                    isDelete = true;
                    Processes.Remove(elem);
                }
            }

            return isDelete;
        }

        public async void RunEvents()
        {
            foreach (var eve in events)
            {
                await eve();
            }
        }

        private async Task _Processor()
        {
            while (Status != ManagerStatus.aborted)
            {
                RunEvents();

                var process = await ProcessesQueue.DequeueAsync();
                if (process == null)
                {
                    await Task.Delay(quantTimeMS);
                }
                else
                {
                    await Task.Delay(quantTimeMS);
                    process.Size -= 1;

                    if (process.IsAlive)
                    {
                        await ProcessesQueue.EnqueueAsync(process, process.Priority);
                    }

                }
            }
        }
    }

    public enum ManagerStatus
    {
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
        public double CompletePercentage { get => 100.0 - (Size * 100.0 / InitialSize); }

        public override string ToString()
        {
            return $"Process {Name}: Id:{Id}, Priority:{Priority}, InitialSize:{InitialSize}";
        }

        //public Process(int id, string name, int priority, int initialSize)
        //{
        //    Id = id;
        //    Name = name;
        //    Priority = priority;
        //    Size = InitialSize = initialSize;
        //}
    }

    public class ConcurrentPriorityQueue<TElement, TKey>
        where TElement : class
        where TKey : IComparable<TKey>
    {
        private readonly PriorityQueue<TElement, TKey> _queue = new PriorityQueue<TElement, TKey>();
        private bool _queueIsFree = true;

        public async Task EnqueueAsync(TElement process, TKey priority)
        {
            await Task.Factory.StartNew(() =>
            {
                while (!_queueIsFree) ;
            });

            _queueIsFree = false;
            _queue.Enqueue(process, priority);
            _queueIsFree = true;
        }

        public async Task<TElement> DequeueAsync()
        {
            await Task.Factory.StartNew(() =>
            {
                while (!_queueIsFree) ;
            });

            _queueIsFree = false;
            var process = _queue.Count == 0 ? null : _queue.Dequeue();
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
            var size = _queue.Count;
            _queueIsFree = true;

            return size;
        }

        public async Task<List<TElement>> GetListAsync()
        {
            await Task.Factory.StartNew(() =>
            {
                while (!_queueIsFree) ;
            });

            _queueIsFree = false;
            var list = _queue.GetList();
            _queueIsFree = true;

            return list;
        }

        public async Task ResetAsync()
        {
            await Task.Factory.StartNew(() =>
            {
                while (!_queueIsFree) ;
            });

            _queueIsFree = false;
            _queue.Reset();
            _queueIsFree = true;

            return;
        }
    }

    public class PriorityQueue<TElement, TKey>
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

        public PriorityQueue()
        {
            Count = 0;
        }

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
            if (Count == 0)
            {
                return null;
            }

            var elem = _Last;
            _Last = elem.prev == _Tale ? null : elem.prev;
            elem.prev.next = null;
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

        public void Reset()
        {
            _Tale.next = null;
            _Last = null;
            Count = 0;
        }
    }
}
