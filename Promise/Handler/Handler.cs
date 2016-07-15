using System;

public struct Handler<E> {
	public Action<E> callback;
	public IRejectable rejectable;
}