﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;

namespace ComponentBind
{
	public interface IProperty
	{
		void AddBinding(IPropertyBinding binding);
		void RemoveBinding(IPropertyBinding binding);
		void Reset();
		Type PropertyType();
	}

	public class PropertyEntry
	{
		public IProperty Property;
		public EditorData Data;

		public PropertyEntry(IProperty property, string description)
		{
			this.Property = property;
			this.Data = new EditorData();
			this.Data.Description = description;
		}

		public PropertyEntry(IProperty property, EditorData data)
		{
			this.Property = property;
			this.Data = data;
		}

		public class EditorData
		{
			public Property<bool> Visible;
			public string Description;
			public IListProperty Options;
			public bool Readonly;
			public bool RefreshOnChange;
			public int IChangeBy = 1;
			public float FChangeBy = 1f;
			public byte BChangeBy = 1;
		}
	}

	[DebuggerDisplay("Property {Value}")]
	public class Property<Type> : IProperty
	{
		[XmlIgnore]
		public Type InternalValue;

		[XmlIgnore]
		public Func<Type> Get;

		protected Action<Type> set;
		[XmlIgnore]
		public Action<Type> Set
		{
			get
			{
				return this.set;
			}
			set
			{
				this.set = value;
				if (this.InternalValue != null && !this.InternalValue.Equals(default(Type)))
					this.set(this.InternalValue);
			}
		}
		protected List<IPropertyBinding> bindings = new List<IPropertyBinding>();

		public void AddBinding(IPropertyBinding binding)
		{
			if (!this.bindings.Contains(binding))
				this.bindings.Add(binding);
		}

		public void RemoveBinding(IPropertyBinding binding)
		{
			this.bindings.Remove(binding);
		}

		public void Changed()
		{
			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].OnChanged(this);
		}

		public Type Value
		{
			get
			{
				return this.InternalGet(null);
			}
			set
			{
				this.InternalSet(value, null);
			}
		}

		public void Reset()
		{
			this.InternalSet(this.InternalGet(null), null);
		}

		public System.Type PropertyType()
		{
			return typeof(Type);
		}

		public void InternalSet(Type obj, IPropertyBinding binding)
		{
			if (this.Set != null)
				this.Set(obj);
			else
				this.InternalValue = obj;

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
			{
				IPropertyBinding b = this.bindings[j];
				if (b != binding)
					b.OnChanged(this);
			}
		}

		public Type InternalGet(IPropertyBinding binding)
		{
			return this.Get != null ? this.Get() : this.InternalValue;
		}

		public static implicit operator Type(Property<Type> obj)
		{
			return obj.Value;
		}

		public override string ToString()
		{
			return this.Value.ToString();
		}
	}

	public interface IListProperty : IProperty
	{
		void CopyTo(IListProperty dest);
	}

	public class ListProperty<Type> : ICollection<Type>, IListProperty
	{
		public delegate void ItemAddedEventHandler(int index, Type t);
		public delegate void ItemRemovedEventHandler(int index, Type t);
		public delegate void ItemChangedEventHandler(int index, Type old, Type newValue);
		public delegate void ClearEventHandler();
		protected bool editable = false;
		[XmlAttribute]
		[DefaultValue(true)]
		public bool Editable
		{
			get
			{
				return this.editable;
			}
			set
			{
				this.editable = value;
			}
		}

		protected string description = "";
		[XmlIgnore]
		[DefaultValue("")]
		public string Description
		{
			get { return description; }
			set { description = value; }
		}

		protected bool serialize = true;
		[XmlIgnore]
		public bool Serialize
		{
			get
			{
				return this.serialize;
			}
			set
			{
				this.serialize = value;
			}
		}

		public int Count
		{
			get
			{
				return this.list.Count;
			}
		}

		public Property<int> Length = new Property<int>();

		public event ItemAddedEventHandler ItemAdded;
		public event ItemRemovedEventHandler ItemRemoved;
		public event ItemChangedEventHandler ItemChanged;
		public event ClearEventHandler Cleared;
		public event ClearEventHandler Clearing;

		public void Reset()
		{

		}

		public System.Type PropertyType()
		{
			return typeof (Type);
		}

		private List<Type> list = new List<Type>();
		protected List<IListBinding<Type>> bindings = new List<IListBinding<Type>>();

		public void AddBinding(IPropertyBinding binding)
		{
			if (!this.bindings.Contains(binding))
				this.bindings.Add((IListBinding<Type>)binding);
		}

		public void RemoveBinding(IPropertyBinding binding)
		{
			this.bindings.Remove((IListBinding<Type>)binding);
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public bool Contains(Type t)
		{
			return this.list.Contains(t);
		}

		public void CopyTo(Type[] array, int arrayIndex)
		{
			this.list.CopyTo(array, arrayIndex);
		}

		IEnumerator<Type> IEnumerable<Type>.GetEnumerator()
		{
			return this.list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.list.GetEnumerator();
		}

		public Type this[int i]
		{
			get
			{
				return this.list[i];
			}
			set
			{
				this.Changed(i, value);
			}
		}

		public void Changed()
		{
			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].OnChanged(this);
		}

		public void CopyTo(IListProperty dest)
		{
			ListProperty<Type> list = (ListProperty<Type>)dest;
			list.Clear();
			foreach (Type t in this)
				list.Add(t);
		}

		public void Add(Type t)
		{
			this.list.Add(t);

			this.Length.Value = this.list.Count;

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].Add(t, this);

			if (this.ItemAdded != null)
				this.ItemAdded(this.list.Count - 1, t);
		}

		public int IndexOf(Type t)
		{
			return this.list.IndexOf(t);
		}

		public void AddAll(IEnumerable<Type> items)
		{
			foreach (Type t in items)
				this.Add(t);
		}

		public void Insert(int index, Type t)
		{
			this.list.Insert(index, t);

			this.Length.Value = this.list.Count;

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].Add(t, this);

			if (this.ItemAdded != null)
				this.ItemAdded(index, t);
		}

		public void RemoveAt(int index)
		{
			Type t = this.list[index];
			this.list.RemoveAt(index);
			this.Length.Value = this.list.Count;
			if (this.ItemRemoved != null)
				this.ItemRemoved(index, t);

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].Remove(t, this);
		}

		public bool Remove(Type t)
		{
			int index = this.list.IndexOf(t);

			this.list.RemoveAt(index);
			this.Length.Value = this.list.Count;
			if (this.ItemRemoved != null)
				this.ItemRemoved(index, t);

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].Remove(t, this);
			
			return true;
		}

		public void RemoveWithoutNotifying(Type t)
		{
			int index = this.list.IndexOf(t);

			this.list.RemoveAt(index);
			this.Length.Value = this.list.Count;

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].Remove(t, this);
		}

		public void RemoveAll(IEnumerable<Type> items)
		{
			foreach (Type t in items)
				this.Remove(t);
		}

		public void Changed(Type from, Type to)
		{
			int i = this.list.IndexOf(from);
			this.list[i] = to;
			if (this.ItemChanged != null)
				this.ItemChanged(i, from, to);

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].OnChanged(from, to, this);
		}

		public void Changed(int i, Type to)
		{
			Type from = this.list[i];
			this.list[i] = to;
			if (this.ItemChanged != null)
				this.ItemChanged(i, from, to);

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].OnChanged(from, to, this);
		}

		public void Clear()
		{
			bool notify = this.list.Count > 0;

			if (notify && this.Clearing != null)
				this.Clearing();

			this.list.Clear();
			this.Length.Value = 0;

			if (notify)
			{
				if (this.Cleared != null)
					this.Cleared();

				for (int i = this.bindings.Count - 1; i >= 0; i = Math.Min(this.bindings.Count - 1, i - 1))
					this.bindings[i].Clear(this);
			}
		}

		public void Changed(Type t)
		{
			if (this.ItemChanged != null)
			{
				int i = this.list.IndexOf(t);
				this.ItemChanged(i, t, t);
			}

			for (int j = this.bindings.Count - 1; j >= 0; j = Math.Min(this.bindings.Count - 1, j - 1))
				this.bindings[j].OnChanged(t, t, this);
		}
	}
}
