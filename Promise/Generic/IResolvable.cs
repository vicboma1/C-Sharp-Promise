using System;


public interface IResolvable<T> : IRejectable {
	void Resolve(T value);
}