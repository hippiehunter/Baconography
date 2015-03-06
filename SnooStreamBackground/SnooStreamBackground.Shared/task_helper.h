#pragma once

#include <ppltasks.h>


namespace task_helper
{

	template<typename TASK>
	struct task_result_type
	{
		typedef typename TASK::result_type result_type;
	};

	template<>
	struct task_result_type<void>
	{
		typedef void result_type;
	};

	template<typename RESULT_TYPE, typename EXCEPTION_TYPE>
	concurrency::task<RESULT_TYPE> default_error_handler(EXCEPTION_TYPE ex)
	{
		return concurrency::task_from_exception<RESULT_TYPE>(ex);
	}


	template<typename TASK, typename HANDLER,
		typename TASK_RESULT = decltype(concurrency::create_task(std::declval<TASK>()))::result_type,
		typename HANDLER_RESULT_TASK = std::result_of<HANDLER(TASK_RESULT)>::type>
		auto continue_task(TASK continueFrom, HANDLER handler) -> HANDLER_RESULT_TASK
	{
		typedef typename task_result_type<HANDLER_RESULT_TASK>::result_type HANDLER_RESULT;
		return concurrency::create_task(continueFrom).then([=](concurrency::task<TASK_RESULT> resultTask)
		{
			try
			{
				auto result = resultTask.get();
				return handler(result);
			}
			catch (concurrency::task_canceled)
			{
				return default_error_handler<HANDLER_RESULT>(ref new Platform::OperationCanceledException());
			}
			catch (Platform::Exception^ ex)
			{
				return default_error_handler<HANDLER_RESULT>(ex);
			}
		});
	}

	template<typename TASK, typename HANDLER, typename TASK_RESULT = decltype(concurrency::create_task(std::declval<TASK>()))::result_type>
	auto continue_void_task(TASK continueFrom, HANDLER handler) -> concurrency::task<void>
	{
		typedef void HANDLER_RESULT;
		return concurrency::create_task(continueFrom).then([=](concurrency::task<TASK_RESULT> resultTask)
		{
			try
			{
				auto result = resultTask.get();
				handler(result);
				return concurrency::task_from_result();
			}
			catch (concurrency::task_canceled)
			{
				return concurrency::task_from_exception<void>(ref new Platform::OperationCanceledException());
			}
			catch (Platform::Exception^ ex)
			{
				return concurrency::task_from_exception<void>(ex);
			}
		});
	}

	template<typename TASK, typename HANDLER, typename ERROR_HANDLER,
		typename TASK_RESULT = decltype(concurrency::create_task(std::declval<TASK>()))::result_type,
		typename HANDLER_RESULT_TASK = std::result_of<HANDLER(TASK_RESULT)>::type>
		auto continue_task(TASK continueFrom, HANDLER handler, ERROR_HANDLER errorHandler) -> HANDLER_RESULT_TASK
	{
		typedef typename task_result_type<HANDLER_RESULT_TASK>::result_type HANDLER_RESULT;
		return concurrency::create_task(continueFrom).then([=](concurrency::task<TASK_RESULT> resultTask)
		{
			try
			{
				auto result = resultTask.get();
				return handler(result);
			}
			catch (concurrency::task_canceled)
			{
				return errorHandler(ref new Platform::OperationCanceledException());
			}
			catch (Platform::Exception^ ex)
			{
				return errorHandler(ex);
			}
		});
	}

	template<typename TASK, typename HANDLER, typename ERROR_HANDLER, typename TASK_RESULT = decltype(concurrency::create_task(std::declval<TASK>()))::result_type>
	auto continue_void_task(TASK continueFrom, HANDLER handler, ERROR_HANDLER errorHandler) -> concurrency::task<void>
	{
		typedef void HANDLER_RESULT;
		return concurrency::create_task(continueFrom).then([=](concurrency::task<TASK_RESULT> resultTask)
		{
			try
			{
				auto result = resultTask.get();
				handler(result);
				return concurrency::task_from_result();
			}
			catch (concurrency::task_canceled)
			{
				return errorHandler(ref new Platform::OperationCanceledException());
			}
			catch (Platform::Exception^ ex)
			{
				return errorHandler(ex);
			}
		});
	}


	template<typename TASK, typename HANDLER, typename ERROR_HANDLER, typename TASK_EXTRAS,
		typename TASK_RESULT = decltype(concurrency::create_task(std::declval<TASK>()))::result_type,
		typename HANDLER_RESULT_TASK = std::result_of<HANDLER(TASK_RESULT)>::type>
		auto continue_task(TASK continueFrom, HANDLER handler, ERROR_HANDLER errorHandler, TASK_EXTRAS extras) -> HANDLER_RESULT_TASK
	{
		typedef typename task_result_type<HANDLER_RESULT_TASK>::result_type HANDLER_RESULT;
		return concurrency::create_task(continueFrom, extras).then([=](concurrency::task<TASK_RESULT> resultTask)
		{
			try
			{
				auto result = resultTask.get();
				return handler(result);
			}
			catch (concurrency::task_canceled)
			{
				return errorHandler(ref new Platform::OperationCanceledException());
			}
			catch (Platform::Exception^ ex)
			{
				return errorHandler(ex);
			}
		});
	}

	template<typename HANDLER, typename ERROR_HANDLER,
		typename TASK_RESULT,
		typename HANDLER_RESULT_TASK = std::result_of<HANDLER(TASK_RESULT)>::type>
		auto continue_task(concurrency::task<TASK_RESULT> continueFrom, HANDLER handler, ERROR_HANDLER errorHandler) -> HANDLER_RESULT_TASK
	{
		typedef typename task_result_type<HANDLER_RESULT_TASK>::result_type HANDLER_RESULT;
		return continueFrom.then([=](concurrency::task<TASK_RESULT> resultTask)
		{
			try
			{
				auto result = resultTask.get();
				return handler(result);
			}
			catch (concurrency::task_canceled)
			{
				return errorHandler(ref new Platform::OperationCanceledException());
			}
			catch (Platform::Exception^ ex)
			{
				return errorHandler(ex);
			}
		});
	}

	template<typename TASK, typename HANDLER, typename ERROR_HANDLER, typename TASK_EXTRAS, typename TASK_RESULT = decltype(concurrency::create_task(std::declval<TASK>()))::result_type>
	auto continue_void_task(TASK continueFrom, HANDLER handler, ERROR_HANDLER errorHandler, TASK_EXTRAS extras) -> concurrency::task<void>
	{
		typedef void HANDLER_RESULT;
		return concurrency::create_task(continueFrom, extras).then([=](concurrency::task<TASK_RESULT> resultTask)
		{
			try
			{
				auto result = resultTask.get();
				handler(result);
				return concurrency::task_from_result();
			}
			catch (concurrency::task_canceled)
			{
				return errorHandler(ref new Platform::OperationCanceledException());
			}
			catch (Platform::Exception^ ex)
			{
				return errorHandler(ex);
			}
		});
	}

	template<typename TASK, typename ERROR_HANDLER, typename TASK_RESULT = decltype(concurrency::create_task(std::declval<TASK>()))::result_type>
	auto finish_task(TASK continueFrom, ERROR_HANDLER errorHandler) -> void
	{
		typedef void HANDLER_RESULT;
		continueFrom.then([=](concurrency::task<TASK_RESULT> resultTask) -> void
		{
			try
			{
				resultTask.get();
			}
			catch (concurrency::task_canceled)
			{
				errorHandler(ref new Platform::OperationCanceledException());
			}
			catch (Platform::Exception^ ex)
			{
				errorHandler(ex);
			}
		});
	}
}