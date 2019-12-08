using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CausewayStudios.Tools.InspectorToolbar
{
    public class DropOutStack<T>
    {
        private const int DEFAULT_CAPACITY = 10;

        private T[] items;
        private int top = 0;
        private int count;

        public DropOutStack(int capacity = DEFAULT_CAPACITY) {
            items = new T[capacity];
        }

        public void Push(T item)
        {
            items[top] = item;
            top = (top + 1) % items.Length;

            count++;
            if (count > items.Length)
            {
                count = items.Length;
            }
        }

        public T Pop()
        {
            top = (items.Length + top - 1) % items.Length;
            T toReturn = items[top];
            items[top] = default(T);
            count--;

            if (count < 0)
            {
                count = 0;
            }

            return toReturn;
        }

        public void Clear()
        {
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = default(T);
            }
        }

        public bool IsEmpty()
        {
            return count == 0;
        }

        public T[] GetStack()
        {
            T[] stack = new T[count];
            int number = 0;
            int i = (items.Length + top - 1) % items.Length;

            while (number < count)
            {
                stack[number] = items[i];
                i = (items.Length + i - 1) % items.Length;
                number++;
            }

            return stack;
        }

        // This doesn't work when making it smaller than the current count
        public void Resize(int newCapacity)
        {
            T[] stack = GetStack();

            items = new T[newCapacity];
            for (int i = count - 1, j = 0; i >= 0; i--, j++)
            {
                items[j] = stack[i];
            }

            top = count;
        }
    }
}
