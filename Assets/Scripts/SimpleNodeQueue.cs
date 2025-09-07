using System;
using System.Collections.Generic;

// simple priority queue (ascending by provided f selector)
public class SimpleNodeQueue<T>
{
    public LinkedList<T> list = new LinkedList<T>(); // kept public to match your .list usage
    private readonly Func<T, float> getF;

    public SimpleNodeQueue(Func<T, float> getF)
    {
        this.getF = getF;
    }

    public void Enqueue(T newItem)
    {
        // if the list is empty, this is the first element
        if (list.Count == 0)
        {
            list.AddFirst(newItem);
            return;
        }
        
        // we need to insert it into its proper priority
        for (LinkedListNode<T> node = list.First; node != null; node = node.Next)
        {
            if (getF(node.Value) > getF(newItem))
            {
                list.AddBefore(node, newItem);
                return;
            }
        }
        
        // if we reach this place, this element has the biggest priority, so it goes to the end of the list
        list.AddLast(newItem);
    }

    public T Dequeue()
    {
        T first = list.First.Value;
        list.RemoveFirst();
        return first;
    }

    public bool IsNotEmpty()
    {
        return list.Count > 0;
    }

    public bool Contains(T item)
    {
        return list.Contains(item);
    }

    public void UpdatePriority(T item)
    {
        LinkedListNode<T> n = list.Find(item);
        if (n == null) return;
        list.Remove(n);
        Enqueue(item);
    }
}