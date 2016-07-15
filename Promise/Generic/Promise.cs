using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class Promise<T> : IPromise<T>
{
	protected static MappingStatePromise<T,Exception> mappingStatePromise;
	private Exception rejectionException;

	private T resolveValue;

	public int Id { get; set; }
	public string Name { get;  set; }
	public PromiseStateEnum State { get;  set; }

	public static IPromise<T> Create(){
		return new Promise<T>();
	}

	public static IPromise<T> Create(Action<Action<T>, Action<Exception>> resolver){
		return new Promise<T>(resolver);
	}

	Promise() {
		if (mappingStatePromise == null)
			mappingStatePromise = MappingStatePromise<T,Exception>.Create<T> ();
		
		this.State = PromiseStateEnum.PENDING;
		this.Id = ++Promise.nextPromiseId;

		if (Promise.EnablePromiseTracking)
			Promise.pendingPromises.Add(this);
	}
		
	Promise(Action<Action<T>, Action<Exception>> resolver) : this() {
		try { 
			resolver( value => Resolve(value), ex => Reject(ex)); 
		}
		catch (Exception ex){
			Reject(ex);
		}
	}

	private void isNotPending () {
		if (State != PromiseStateEnum.PENDING)
			throw new ApplicationException ("Attempt to reject a promise that is already in state: " + State + ", a promise can only be rejected when it is still in state: " + PromiseStateEnum.PENDING);	
	}


	public void Reject(Exception e) {

		this.isNotPending ();

		rejectionException = e;
		State = PromiseStateEnum.REJECTED;

		if (Promise.EnablePromiseTracking)
			Promise.pendingPromises.Remove(this);

		mappingStatePromise.RejectHandlers(e,this);
	}

	public void Resolve(T value) {
		this.isNotPending ();

		resolveValue = value;
		State = PromiseStateEnum.RESOLVED;

		if (Promise.EnablePromiseTracking)
			Promise.pendingPromises.Remove(this);

		mappingStatePromise.ResolveHandlers(value,this);
	}

	public void Done(Action<T> onResolved, Action<Exception> onRejected){
		Then(onResolved, onRejected)
			.Catch(ex =>
				Promise.PropagateUnhandledException(this, ex)
			);
	}

	public void Done(Action<T> onResolved) {
		Then(onResolved)
				.Catch(ex => Promise.PropagateUnhandledException(this, ex));
	}

	public void Done() {
		Catch(ex => Promise.PropagateUnhandledException(this, ex));
	}
		
	public IPromise<T> Catch(Action<Exception> onRejected) {
		var resultPromise = new Promise<T>();

		Action<T> resolveHandler = v => {
			resultPromise.Resolve(v);
		};

		Action<Exception> rejectHandler = ex => {
			onRejected(ex);
			resultPromise.Reject(ex);
		};

		this.ActionHandlers(resultPromise, resolveHandler, rejectHandler);

		return resultPromise;
	}

	public IPromise<Z> Then<Z>(Func<T, IPromise<Z>> onResolved) {
		return Then(onResolved, null);
	}

	public IPromise<T> Then(Action<T> onResolved) {
		return Then(onResolved, null);
	}

	public IPromise<Z> Then<Z>(Func<T, IPromise<Z>> onResolved, Action<Exception> onRejected) {
		var resultPromise = new Promise<Z>();

		Action<T> resolveHandler = v => {
			onResolved(v)
				.Then(
					(Z castValue) => resultPromise.Resolve(castValue),
					ex => resultPromise.Reject(ex)
				);
		};

		Action<Exception> rejectHandler = ex => {
			if (onRejected != null)
				onRejected(ex);

			resultPromise.Reject(ex);
		};

		this.ActionHandlers(resultPromise, resolveHandler, rejectHandler);

		return resultPromise;
	}



	public IPromise<Z> Then<Z>(Func<IPromise<Z>> onResolved, Action<Exception> onRejected) {
		var resultPromise = new Promise<Z>();

		ActionHandlers(
			resultPromise,
			(s) => { 
				onResolved()
					.Then( 
						(Z chainedValue) => resultPromise.Resolve(chainedValue),
						ex => resultPromise.Reject(ex)
					);
			}
			, 
			ex => {
				if (onRejected != null)
					onRejected(ex);

				resultPromise.Reject(ex);
			});

		return resultPromise;
	}

	public IPromise<T> Then(Action<T> onResolved, Action<Exception> onRejected) {
		var resultPromise = new Promise<T>();
		Action<T> resolveHandler = v => {
			if (onResolved != null)
				onResolved(v);
	
			resultPromise.Resolve(v);
		};

		Action<Exception> rejectHandler = ex => {
			if (onRejected != null)
				onRejected(ex);
	
			resultPromise.Reject(ex);
		};

		ActionHandlers(resultPromise, resolveHandler, rejectHandler);

		return resultPromise;
	}

	public IPromise<Z> Then<Z>(Func<T, Z> transform) {
		return Then(value => Promise<Z>.Resolved(transform(value)));
	}


	public void ActionHandlers(IRejectable resultPromise, Action<T> resolveHandler, Action<Exception> rejectHandler) {
		mappingStatePromise
			.Get (State)
			.Invoke (resultPromise, resolveHandler, resolveValue, rejectHandler, rejectionException, this);
	}

	public IPromise<IEnumerable<Z>> ThenAll<Z>(Func<T, IEnumerable<IPromise<Z>>> chain) {
		return Then(value => Promise<Z>.All(chain(value)));
	}
		
	public static IPromise<IEnumerable<T>> All(params IPromise<T>[] promises) {
		return All((IEnumerable<IPromise<T>>)promises); // Cast is required to force use of the other All function.
	}
	
	public static IPromise<IEnumerable<T>> All(IEnumerable<IPromise<T>> enumerable) {
		var promisesArray = UtilsPromise.ToArray (enumerable);
		if (promisesArray.Length == 0)
			return Promise<IEnumerable<T>>.Resolved(EnumerablePromise._Empty<T>());
		

		var remainingCount = promisesArray.Length;
		var results = new T[remainingCount];
		var resultPromise = new Promise<IEnumerable<T>>();

		promisesArray.Each((promise, index) => {
			promise
				.Catch(ex => {
					if (resultPromise.State == PromiseStateEnum.PENDING)
						resultPromise.Reject(ex);	
				})
				.Then(result => {
					results[index] = result;

					--remainingCount;
					if (remainingCount <= 0)
					resultPromise.Resolve(results);
				})
				.Done();
		});

		return resultPromise;
	}

	public IPromise<Z> Once<Z>(Func<T, IEnumerable<IPromise<Z>>> chain){
		return Then(value => Promise<Z>.Once(chain(value)));
	}
		
	public static IPromise<T> Once(params IPromise<T>[] promises) {
		return Once((IEnumerable<IPromise<T>>)promises); // Cast is required to force use of the other function.
	}
	
	public static IPromise<T> Once(IEnumerable<IPromise<T>> promises) {
		var promisesArray = promises.ToArray();
		if (promisesArray.Length == 0)
			throw new ApplicationException("At least 1 input promise must be provided for Race");

		var resultPromise = new Promise<T>();

		promisesArray.Each((promise, index) =>
		{
			promise
				.Catch(ex => {
						if (resultPromise.State == PromiseStateEnum.PENDING)
							resultPromise.Reject(ex);
					})
				.Then(result => {
						if (resultPromise.State == PromiseStateEnum.PENDING)
							resultPromise.Resolve(result);
					})
				.Done();
		});

		return resultPromise;
	}

	public static IPromise<T> Resolved(T promisedValue) {
		var promise = new Promise<T>();
		promise.Resolve(promisedValue);
		return promise;
	}

	public static IPromise<T> Rejected(Exception e) {
		var promise = new Promise<T>();
		promise.Reject(e);
		return promise;
	}
}