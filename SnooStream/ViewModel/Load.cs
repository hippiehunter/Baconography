using GalaSoft.MvvmLight;
using SnooSharp;
using SnooStream.Common;
using SnooStream.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.Web;

namespace SnooStream.ViewModel
{
    public enum LoadState
    {
        None,
        Refreshing,
        Loading,
        Loaded,
        Cancelled,
        NoItems,
        NotFound,
        Disallowed,
        NetworkFailure,
        NetworkCaptured,
        Failure,
        NotAuthorized,
    }

    public enum LoadKind
    {
        Collection,
        Item
    }

    public interface IHasLoadableState
    {
        LoadViewModel LoadState { get; }
    }

    public class LoadViewModel : SnooObservableObject
    {
        public LoadKind Kind { get; set; } = LoadKind.Item;

        public string Url { get; set; }

        private LoadState _state;
        public LoadState State
        {
            get
            {
                return _state;
            }
            set
            {
                Set("State", ref _state, value);
            }
        }
        public float LoadPercent { get; set; } = 0;
        public Func<IProgress<float>, CancellationToken, Task> LoadAction { get; set; }
        public CancellationToken? CancelToken { get; set; }
        public bool IsCritical { get; set; }
        private CancellationTokenSource _internalCancelToken = new CancellationTokenSource();

        public static LoadViewModel ReplaceLoadViewModel(LoadViewModel existing, LoadViewModel newViewModel)
        {
            if (existing != null)
                existing.Cancel();

            return newViewModel;
        }

        public async void Retry()
        {
            _internalCancelToken = new CancellationTokenSource();
            _loadTask = null;
            await LoadAsync();
        }

        public void Cancel()
        {
            _internalCancelToken.Cancel();
        }

        public async void Load()
        {
            await LoadAsync();
        }

        Task _loadTask;

        public Task LoadAsync()
        {
            if (_loadTask == null)
            {
                lock(this)
                {
                    if (_loadTask == null)
                    {
                        _loadTask = LoadAsyncImpl();
                    }
                }
            }
            return _loadTask;
        }

        private async Task LoadAsyncImpl()
        {
            try
            {
                if (State != LoadState.Loaded)
                {
                    State = LoadState.Loading;
                    var progress = new AggregateProgress(value => { LoadPercent = value; RaisePropertyChanged("LoadPercent"); });
                    await LoadAction(progress, CancelToken != null ? CancellationTokenSource.CreateLinkedTokenSource(CancelToken.Value, _internalCancelToken.Token).Token : _internalCancelToken.Token);
                    LoadAction = null;
                    State = LoadState.Loaded;
                    _internalCancelToken.Cancel();
                    _internalCancelToken.Dispose();
                    _internalCancelToken = null;
                }
            }
            catch (OperationCanceledException)
            {
                State = LoadState.Cancelled;
            }
            catch (RedditEmptyException)
            {
                State = LoadState.NoItems;
            }
            catch (RedditNotFoundException)
            {
                State = LoadState.NotFound;
            }
            catch (RedditDisallowedException)
            {
                State = LoadState.NotAuthorized;
            }
            catch (RedditException)
            {
                State = LoadState.Failure;
            }
            catch (Exception ex)
            {
                switch (WebError.GetStatus(ex.HResult))
                {
                    case WebErrorStatus.CertificateCommonNameIsIncorrect:
                    case WebErrorStatus.CertificateExpired:
                    case WebErrorStatus.CertificateContainsErrors:
                    case WebErrorStatus.CertificateRevoked:
                    case WebErrorStatus.CertificateIsInvalid:
                    case WebErrorStatus.HttpToHttpsOnRedirection:
                    case WebErrorStatus.HttpsToHttpOnRedirection:
                    case WebErrorStatus.ProxyAuthenticationRequired:
                    case WebErrorStatus.UseProxy:
                        State = LoadState.NetworkCaptured;
                        break;
                    case WebErrorStatus.ServerUnreachable:
                    case WebErrorStatus.Timeout:
                    case WebErrorStatus.ErrorHttpInvalidServerResponse:
                    case WebErrorStatus.ConnectionAborted:
                    case WebErrorStatus.ConnectionReset:
                    case WebErrorStatus.Disconnected:
                    case WebErrorStatus.CannotConnect:
                    case WebErrorStatus.HostNameNotResolved:
                    case WebErrorStatus.UnexpectedStatusCode:
                    case WebErrorStatus.UnexpectedRedirection:
                    case WebErrorStatus.UnexpectedClientError:
                    case WebErrorStatus.GatewayTimeout:
                    case WebErrorStatus.HttpVersionNotSupported:
                    case WebErrorStatus.ExpectationFailed:
                    case WebErrorStatus.RedirectFailed:
                    case WebErrorStatus.RequestTimeout:
                    case WebErrorStatus.BadGateway:
                        State = LoadState.NetworkFailure;
                        break;
                    case WebErrorStatus.OperationCanceled:
                        State = LoadState.Cancelled;
                        break;
                    case WebErrorStatus.MultipleChoices:
                    case WebErrorStatus.MovedPermanently:
                    case WebErrorStatus.Found:
                    case WebErrorStatus.SeeOther:
                    case WebErrorStatus.NotModified:
                    case WebErrorStatus.NotFound:
                    case WebErrorStatus.Gone:
                    case WebErrorStatus.TemporaryRedirect:
                    case WebErrorStatus.BadRequest:
                        State = LoadState.NotFound;
                        break;
                    case WebErrorStatus.Unauthorized:
                    case WebErrorStatus.PaymentRequired:
                    case WebErrorStatus.Forbidden:
                    case WebErrorStatus.MethodNotAllowed:
                    case WebErrorStatus.NotAcceptable:
                        State = LoadState.NotAuthorized;
                        break;
                    case WebErrorStatus.Conflict:
                    case WebErrorStatus.LengthRequired:
                    case WebErrorStatus.PreconditionFailed:
                    case WebErrorStatus.RequestEntityTooLarge:
                    case WebErrorStatus.RequestUriTooLong:
                    case WebErrorStatus.UnsupportedMediaType:
                    case WebErrorStatus.RequestedRangeNotSatisfiable:
                    case WebErrorStatus.InternalServerError:
                    case WebErrorStatus.NotImplemented:
                    case WebErrorStatus.ServiceUnavailable:
                    case WebErrorStatus.UnexpectedServerError:
                    case WebErrorStatus.Unknown:
                    default:
                        State = LoadState.Failure;
                        break;
                }
            }
            finally
            {
                _loadTask = null;
            }
        }
    }
}
