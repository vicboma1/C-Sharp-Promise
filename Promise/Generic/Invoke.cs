using System;

public class Invoke
{
	public static IPromise<E> Handler<E,Z>(Action<Z> callback, IRejectable rejectable, Z value, IPromise<E> _promise) {
		try {
			callback(value);
		}
		catch (Exception ex) {
			rejectable.Reject(ex);
		}

		return _promise;
	}

}

