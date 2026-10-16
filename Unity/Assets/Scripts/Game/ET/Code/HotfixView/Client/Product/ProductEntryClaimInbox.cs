using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        public static string ClaimInboxCaption(this ProductEntryPresentation self) => self.ClaimInboxState switch
        {
            ClaimInboxDisplayState.Loading => "合作待领：查询中 · 取消",
            ClaimInboxDisplayState.Ready => "合作待领：" + self.AvailableClaimCount + " · 查看",
            ClaimInboxDisplayState.Failed => "合作待领：查询失败 · 重试",
            _ => "合作待领：未查询 · 查看"
        };

        private static void ClaimInboxDisplayChanged(this ProductEntryPresentation self)
        {
            ++self.ClaimInboxVersion;
            if (!self.Suspended && ((UIGridKitchen)self.Hud)?.Available == true)
            {
                self.RenderProductEntry();
            }
        }

        public static void InvalidateClaimInbox(this ProductEntryPresentation self)
        {
            ++self.ClaimInboxInvalidation;
            self.ClaimInboxDirty = true;
            self.ClaimInboxState = ClaimInboxDisplayState.Unknown;
            self.ClaimInboxError = null;
            self.ClaimInboxDisplayChanged();
        }

        private static void AcceptClaimInboxRows(this ProductEntryPresentation self, ProgressTicketSummary[] rows)
        {
            // 当前页面的成功Browse可直接更新计数，退役更早的后台显示查询。
            ++self.ClaimInboxSerial;
            self.ClaimInboxRunning = self.ClaimInboxDirty = false;
            self.AvailableClaimCount = rows.Count(row => row.State == ProgressReceiptState.Available);
            self.ClaimInboxState = ClaimInboxDisplayState.Ready;
            self.ClaimInboxError = null;
            self.ClaimInboxDisplayChanged();
        }

        private static void FailClaimInbox(this ProductEntryPresentation self, string error)
        {
            ++self.ClaimInboxSerial;
            self.ClaimInboxRunning = self.ClaimInboxDirty = false;
            self.ClaimInboxState = ClaimInboxDisplayState.Failed;
            self.ClaimInboxError = ProgressError(error);
            self.ClaimInboxDisplayChanged();
        }

        private static void RefreshClaimInbox(this ProductEntryPresentation self)
        {
            // 不为badge创建Profile或repository；显式打开原领取入口才沿原初始化路径。
            SoloSaveRepository repository = self.Root().GetComponent<SoloSaveRepository>();
            long instance = repository?.InstanceId ?? 0;
            if (self.ClaimInboxRepository != instance)
            {
                self.ClaimInboxRepository = instance;
                self.InvalidateClaimInbox();
            }
            if (repository == null || self.ClaimsVisible || !self.ClaimInboxDirty || self.ClaimInboxRunning)
            {
                return;
            }
            self.ClaimInboxDirty = false;
            self.ClaimInboxRunning = true;
            self.ClaimInboxState = ClaimInboxDisplayState.Loading;
            long serial = ++self.ClaimInboxSerial, invalidation = self.ClaimInboxInvalidation;
            EntityRef<ProductEntryPresentation> original = self;
            EntityRef<SoloSaveRepository> originalRepository = repository;
            self.ClaimInboxDisplayChanged();
            self.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    var result = await context.CallLocalValueAsync(self.Root().GetComponent<ProcessInnerSender>(), repository.GetActorId(),
                        ProductMessages.Create<ProgressRepositoryRequest>(value => value.Operation = (int)ProgressRepositoryOperation.Browse), response =>
                        {
                            return (Error: response.Error == 0 ? null : response.Message ?? "RequestRejected",
                                Count: response.Error == 0 ? ((ProgressRepositoryResponse)response).Documents.Count(document =>
                                    ProgressClaimCodec.Decode<ProgressTicketSummary>(document).State == ProgressReceiptState.Available) : 0);
                        });
                    context.ThrowIfStopped();
                    if (result.Error != null)
                    {
                        throw new InvalidOperationException(result.Error);
                    }
                    self = original; repository = originalRepository;
                    if (self == null || repository == null || self.ClaimInboxSerial != serial || self.ClaimInboxInvalidation != invalidation ||
                        self.Root().GetComponent<SoloSaveRepository>() != repository)
                    {
                        return;
                    }
                    self.AvailableClaimCount = result.Count;
                    self.ClaimInboxState = ClaimInboxDisplayState.Ready;
                    self.ClaimInboxError = null;
                }
                catch (OperationCanceledException) { }
                catch (Exception error)
                {
                    self = original;
                    if (self != null && self.ClaimInboxSerial == serial && self.ClaimInboxInvalidation == invalidation)
                    {
                        self.ClaimInboxState = ClaimInboxDisplayState.Failed;
                        self.ClaimInboxError = ProgressError(error.Message);
                    }
                }
                finally
                {
                    self = original;
                    if (self != null && self.ClaimInboxSerial == serial)
                    {
                        self.ClaimInboxRunning = false;
                        self.ClaimInboxDisplayChanged();
                    }
                }
            });
        }

        public static void AddClaimInboxControl(this ProductEntryPresentation self, MonoUIGridKitchen view)
        {
            if (self.ClaimInboxState == ClaimInboxDisplayState.Failed)
            {
                view.StatusText.text = "合作待领查询失败：" + self.ClaimInboxError;
            }
            EntityRef<ProductEntryPresentation> original = self;
            view.AddControl(self.ClaimInboxCaption(), () =>
            {
                ProductEntryPresentation current = original;
                if (current == null)
                {
                    return;
                }
                if (current.ClaimInboxState == ClaimInboxDisplayState.Loading)
                {
                    ++current.ClaimInboxSerial;
                    current.ClaimInboxRunning = current.ClaimInboxDirty = false;
                    current.ClaimInboxState = ClaimInboxDisplayState.Unknown;
                    current.ClaimInboxDisplayChanged();
                    return;
                }
                if (current.ClaimInboxState == ClaimInboxDisplayState.Failed)
                {
                    current.InvalidateClaimInbox();
                    return;
                }
                current.OpenClaims();
            });
        }
    }
}
