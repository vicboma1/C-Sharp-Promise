using System;
using System.Collections.Generic;
using System.Collections;

public class MappingStatePromise<E,Z> where Z : Exception
{
	public delegate T6 Func<T1, T2, T3, T4, T5,T6>(T1 arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);

	private IDictionary<PromiseStateEnum,Func<IRejectable, Action<E>,E, Action<Z>,Z, IPromise<E>>> mapping;

	private List<Handler<Z>> rejectHandlers;
	private List<Action<E>> resolveCallbacks;
	private List<IRejectable> resolveRejectables;

	public static  MappingStatePromise<T,Z> Create<T>(){
		return new MappingStatePromise<T,Z>(new List<Handler<Z>>(),new List<IRejectable>(), new List<Action<T>>());
	}
		
	MappingStatePromise(List<Handler<Z>> rejectHandlers, List<IRejectable> resolveRejectables, List<Action<E>> resolveCallbacks) {
		this.rejectHandlers = rejectHandlers;
		this.resolveRejectables = resolveRejectables;
		this.resolveCallbacks = resolveCallbacks;

		this.mapping = new Dictionary<PromiseStateEnum, Func<IRejectable, Action<E>, E, Action<Z>, Z, IPromise<E>> > (){
			{ 
				PromiseStateEnum.RESOLVED, (resultPromise, resolveHandler, resolveValue, rejectHandler, rejectionException, _promise) => 
					Invoke.Handler<E,E>(resolveHandler, resultPromise, resolveValue, _promise)
			},

			{ 
				PromiseStateEnum.REJECTED, (resultPromise, resolveHandler,  resolveValue, rejectHandler, rejectionException, _promise) => 
					Invoke.Handler<E,Z>(rejectHandler, resultPromise, rejectionException , _promise)
			},

			{
				PromiseStateEnum.PENDING,  (resultPromise, resolveHandler, resolveValue, rejectHandler, rejectionException, _promise) => 
				{
					AddResolveHandler(resolveHandler, resultPromise);
					AddRejectHandler(rejectHandler, resultPromise);

					return _promise;
				}
			}
		};
	}


	private void AddRejectHandler(Action<Z> onRejected, IRejectable rejectable) {
		rejectHandlers.Add (
			new Handler<Z> () { 
				callback = onRejected, rejectable = rejectable 
			}
		);
	}

	private void AddResolveHandler(Action<E> onResolved, IRejectable rejectable) {
		resolveCallbacks.Add(onResolved);
		resolveRejectables.Add(rejectable);
	}
		

	public Func<IRejectable, Action<E>, E, Action<Z>,Z,IPromise<E>> Get(PromiseStateEnum state){
		return this.mapping [state];
	}
		
	public void ResolveHandlers(E value, IPromise<E> promise) {
		for (int i = 0, maxI = resolveCallbacks.Count; i < maxI; i++)
			this.Get(PromiseStateEnum.RESOLVED).Invoke(resolveRejectables[i], resolveCallbacks[i],value, null,null,promise);

		ClearHandlers();
	}

	public void RejectHandlers(Z ex,IPromise<E> promise) {
		rejectHandlers.Each(handler => this.Get(PromiseStateEnum.REJECTED).Invoke( handler.rejectable,null, default(E), handler.callback, ex, promise));
		ClearHandlers();
	}
		
	private void ClearHandlers() {
		rejectHandlers.Clear ();
		resolveCallbacks.Clear ();
		resolveRejectables.Clear ();
	}

}