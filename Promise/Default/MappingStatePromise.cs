using System;
using System.Collections.Generic;
using System.Collections;

public class MappingStatePromise
{
	private IDictionary<PromiseStateEnum,Action<IRejectable, Action, Action<Exception>,Exception>> mapping;
	private List<RejectHandler> rejectHandlers;
	private List<ResolveHandler> resolveHandlers;


	public static MappingStatePromise Create(){
		return new MappingStatePromise (new List<RejectHandler>(),new List<ResolveHandler>());
	}
		
	MappingStatePromise (List<RejectHandler> rejectHandlers, List<ResolveHandler> resolveHandlers ) {
		this.resolveHandlers = resolveHandlers;
		this.rejectHandlers = rejectHandlers;

		this.mapping = new Dictionary<PromiseStateEnum, Action<IRejectable, Action, Action<Exception>,Exception>> (){
			{ 
				PromiseStateEnum.RESOLVED, (resultPromise, resolveHandler, rejectHandler, rejectionException) => {
				
					try {
						resolveHandler.Invoke();
					}
					catch (Exception ex) {
						resultPromise.Reject(ex);
					}

				}
			},

			{ 
				PromiseStateEnum.REJECTED, (resultPromise, resolveHandler, rejectHandler, rejectionException) => {
					try {
						rejectHandler.Invoke(rejectionException);
					}
					catch (Exception ex) {
						resultPromise.Reject(ex);
					}
				}
			
			},

			{
				PromiseStateEnum.PENDING,  (resultPromise, resolveHandler, rejectHandler, rejectionException) => 
				{
					AddResolveHandler(resolveHandler, resultPromise);
					AddRejectHandler(rejectHandler, resultPromise);
				}
			}
		};
	}

	public Action<IRejectable, Action, Action<Exception>,Exception> Get(PromiseStateEnum state){
		return this.mapping [state];
	}


	public void InvokeRejectHandlers(Exception ex) {
		if (rejectHandlers != null)
			rejectHandlers.Each(handler => this.Get(PromiseStateEnum.REJECTED).Invoke( handler.rejectable,null, handler.callback, ex));

		ClearHandlers();
	}

	public void InvokeResolveHandlers() {
		if (resolveHandlers != null)
			resolveHandlers.Each(handler => this.Get(PromiseStateEnum.RESOLVED).Invoke(handler.rejectable, handler.callback, null,null));

		ClearHandlers();
	}


	private void AddRejectHandler(Action<Exception> onRejected, IRejectable rejectable) {
		if(rejectHandlers == null)
			rejectHandlers = new List<RejectHandler>();

		rejectHandlers.Add(new RejectHandler() {
			callback = onRejected,
			rejectable = rejectable
		});
	}

	private void AddResolveHandler(Action onResolved, IRejectable rejectable) {
		if( resolveHandlers == null)
			resolveHandlers = new List<ResolveHandler>();

		resolveHandlers.Add(new ResolveHandler() {
			callback = onResolved,
			rejectable = rejectable
		});
	}

	private void ClearHandlers() {
		rejectHandlers = null;
		resolveHandlers = null;
	}
}