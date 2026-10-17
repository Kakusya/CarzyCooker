using System;
using Game;

namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        private static void NavigateAuthorizationTask(this ProductEntryPresentation self, AuthorizationViewTask task)
        {
            if (self.ProgressCommitting)
            {
                return;
            }
            ++self.ProgressSerial;
            ((UIGridKitchen)self.Hud)?.View.ClearTargets();
            self.Busy = false;
            self.ProgressVisible = true;
            self.ClaimsNeedsRefresh = false;
            self.AuthorizationTask = task;
            self.AuthorizationIdentityExpanded = false;
            self.AuthorizationInspectedPath = null;
            self.AuthorizationResultSlot = self.AuthorizationResultProfile = self.AuthorizationOutputPath = null;
            self.AuthorizationResultRevision = 0;
            self.AuthorizationExport = AuthorizationExportPurpose.IndependentBranch;
            self.ProgressPath = self.ProgressFeedback = self.Failure = null;
            self.ProgressHeaders = Array.Empty<SoloSaveHeader>();
            self.InspectedIdentity = null; self.InspectedAuthorization = null; self.InspectedSummary = null;
            self.InspectedTrusted = false; self.InspectedAuthorizationKind = SaveAuthorizationKind.None;
            self.ConfirmImport = false;
            self.ReplaceTicketId = null; self.ReplacePreview = null; self.ReplaceGrant = null;
            self.ClaimDetailTicketId = self.ClaimDestinationSlot = null;
            self.RenderProductEntry();
        }

        private static bool AuthorizationPathIsCurrent(this ProductEntryPresentation self)
        {
            UIGridKitchen hud = self.Hud;
            if (self.AuthorizationTask != AuthorizationViewTask.Advanced &&
                (hud?.View.EntryAddress ?? "").Trim() != self.AuthorizationInspectedPath)
            {
                self.Failure = "文件路径已变化，请重新检查后再确认";
                self.RenderProductEntry();
                return false;
            }
            return true;
        }

        private static void BeginTaskImport(this ProductEntryPresentation self)
        {
            if (self.InspectedAuthorizationKind != SaveAuthorizationKind.ImportBranch || !self.InspectedTrusted || !self.AuthorizationPathIsCurrent())
            {
                return;
            }
            self.ConfirmImport = true;
            self.RenderProductEntry();
        }

        private static void SelectTaskReplacement(this ProductEntryPresentation self)
        {
            self.OpenClaims();
            self.AuthorizationTask = AuthorizationViewTask.ReplaceRecord;
            self.ProgressVisible = false;
            self.ClaimDetailTicketId = null;
            self.ClaimFilter = ClaimListFilter.Available;
            self.ClaimRows = Array.Empty<ProgressTicketSummary>();
            self.BrowseClaims();
        }

        private static string InspectedAuthorizationPurpose(this ProductEntryPresentation self) => self.InspectedAuthorizationKind switch
        {
            SaveAuthorizationKind.ImportBranch => "独立分支许可",
            SaveAuthorizationKind.Replace => "有损替换许可",
            _ => self.InspectedAuthorization != null ? "房间只读许可" : "公共身份文件"
        };

        // Full identifiers remain visible; separate each field so the portrait summary does not shrink to fit one long line.
        private static string ReadableAuthorizationSummary(string summary) => (summary ?? "")
            .Replace("/", "\n记录：").Replace(" · 修订 ", "\n修订：").Replace(" · ", "\n");

        private static void AddAuthorizationIdentityDetails(this ProductEntryPresentation self, MonoUIGridKitchen view)
        {
            if (self.InspectedIdentity == null)
            {
                return;
            }
            self.AddProgressControl(view, self.AuthorizationIdentityExpanded ? "收起身份明细" : "展开身份明细", current =>
            {
                current.AuthorizationIdentityExpanded = !current.AuthorizationIdentityExpanded;
                current.RenderProductEntry();
            });
            if (self.AuthorizationIdentityExpanded)
            {
                var identity = self.InspectedIdentity;
                view.SetInformation("公共身份明细 · 只读", "名称：" + identity.DisplayName + "\n身份标识：" + identity.ProfileId +
                    "\n公钥标识：\n" + identity.KeyId + "\n已检查文件：" + System.IO.Path.GetFileName(self.AuthorizationInspectedPath) +
                    "\n完整路径见输入框\n用途：" + self.InspectedAuthorizationPurpose(), true, false);
            }
        }

        private static bool RenderAuthorizationTask(this ProductEntryPresentation self, MonoUIGridKitchen view, ClientSessionComponent client, ProductRoomClient room)
        {
            if (self.AuthorizationTask == AuthorizationViewTask.Advanced || self.ClaimDestinationSlot != null)
            {
                return false;
            }
            bool joined = client?.Stage == ProductClientStage.Joined && room?.Current.Version > 0;
            view.PhaseText.text = "存档与授权";
            view.ClockText.text = "按用途操作";
            view.HintText.text = "查看不信任；信任不导入；替换需要单独确认";
            if (self.Busy)
            {
                view.StatusText.text = self.ProgressCommitting ? "正在提交，请等待实际结果…" : "正在读取，可以取消等待";
                if (!self.ProgressCommitting)
                {
                    self.AddProgressControl(view, "取消读取", current =>
                    {
                        ++current.ProgressSerial;
                        current.Busy = false;
                        current.ProgressFeedback = "读取已取消，请核对当前已检查的来源";
                        current.RenderProductEntry();
                    });
                    self.AddProgressControl(view, "返回任务选择", current => current.NavigateAuthorizationTask(AuthorizationViewTask.Home));
                }
                return true;
            }
            if (self.ConfirmImport)
            {
                self.RenderImportConfirmation(view);
                return true;
            }
            self.AddProgressControl(view, "返回任务选择", current => current.NavigateAuthorizationTask(AuthorizationViewTask.Home));
            if (self.AuthorizationTask == AuthorizationViewTask.ImportBranch && self.AuthorizationResultSlot != null)
            {
                view.SettlementText.text = "独立分支已导入\n本机记录：" + self.AuthorizationResultSlot + "\n当前修订：" + self.AuthorizationResultRevision +
                    "\n归属本机身份，源档保留；没有自动开局。";
                self.AddProgressControl(view, "查看导入记录", current =>
                {
                    current.ClaimDestinationSlot = current.AuthorizationResultSlot;
                    current.RenderProductEntry();
                });
                return true;
            }
            if (self.AuthorizationTask == AuthorizationViewTask.Home)
            {
                view.SettlementText.text = "你想做什么？\n继续他人餐厅：只读运行当前房间许可\n导入独立进度：新建归自己的分支\n导出给朋友：先选用途和自己的记录\n替换我的记录：选结果和目标，再单独确认";
                AddTask("继续他人餐厅", AuthorizationViewTask.ContinueRestaurant);
                AddTask("导入独立进度", AuthorizationViewTask.ImportBranch);
                AddTask("导出给朋友", AuthorizationViewTask.ExportToFriend);
                AddTask("替换我的记录", AuthorizationViewTask.ReplaceRecord);
                self.AddProgressControl(view, "身份与高级操作", current => current.NavigateAuthorizationTask(AuthorizationViewTask.Advanced));
                self.AddProgressControl(view, self.ClaimsVisible ? "返回领取列表" : "返回房间", current =>
                {
                    current.ProgressVisible = false;
                    if (current.ClaimsVisible)
                    {
                        current.ClaimsNeedsRefresh = true;
                        current.BrowseClaims();
                    }
                    else
                    {
                        current.RenderProductEntry();
                    }
                });
                return true;
            }
            view.ShowEntryAddress(self.ProgressPath ?? "", 2048);
            if (self.AuthorizationTask == AuthorizationViewTask.ExportToFriend)
            {
                self.RenderAuthorizationExportTask(view, joined);
                self.AddAuthorizationIdentityDetails(view);
                return true;
            }
            string title = self.AuthorizationTask == AuthorizationViewTask.ImportBranch ? "导入独立进度" :
                self.AuthorizationTask == AuthorizationViewTask.ContinueRestaurant ? "继续他人餐厅" : "替换我的记录";
            string effect = self.AuthorizationTask == AuthorizationViewTask.ImportBranch ? "明确导入后新建自己的记录，源档和其他分支保留" :
                self.AuthorizationTask == AuthorizationViewTask.ContinueRestaurant ? "只读来源先发布提案；全员准备后由Host开局，不取得源档覆盖权" : "先选可领取结果及目标，最后单独确认有损替换";
            view.SettlementText.text = title + "\n第1步：填写并检查相应用途的文件路径\n第2步：核对来源，单独确认信任\n第3步：" + effect;
            self.AddProgressControl(view, "查看导入文件", current => current.ProgressFileOperation(ProgressFileAction.Inspect));
            if (joined && self.AuthorizationTask == AuthorizationViewTask.ContinueRestaurant)
            {
                self.AddProgressControl(view, "验证本机身份", current => current.VerifyProgressProfile());
            }
            if (self.AuthorizationTask == AuthorizationViewTask.ReplaceRecord)
            {
                self.AddProgressControl(view, "选择合作结果", current => current.SelectTaskReplacement());
                view.SettlementText.text += "\n替换自己的记录可直接选择结果；外部目标需另行检查替换许可。";
            }
            if (self.InspectedIdentity == null)
            {
                if (self.AuthorizationTask == AuthorizationViewTask.ContinueRestaurant && !joined)
                {
                    view.SettlementText.text += "\n请先从合作入口创建/加入房间，再向档主取得针对本房主和本房间的许可。";
                }
                return true;
            }
            bool matches = self.AuthorizationTask switch
            {
                AuthorizationViewTask.ImportBranch => self.InspectedAuthorizationKind == SaveAuthorizationKind.ImportBranch,
                AuthorizationViewTask.ReplaceRecord => self.InspectedAuthorizationKind == SaveAuthorizationKind.Replace,
                _ => self.InspectedAuthorizationKind == SaveAuthorizationKind.None && self.InspectedAuthorization != null
            };
            view.SettlementText.text = title + "\n已检查用途：" + self.InspectedAuthorizationPurpose() + "\n" + ReadableAuthorizationSummary(self.InspectedSummary) +
                "\n" + (matches ? self.InspectedTrusted ? "该公钥已明确信任；仍需单独确认本次操作" : "尚未信任该公钥；检查文件没有建立信任" : "文件用途不符，请取得本任务所需许可；未执行操作");
            self.AddAuthorizationIdentityDetails(view);
            if (!matches)
            {
                return true;
            }
            if (!self.InspectedTrusted)
            {
                self.AddProgressControl(view, "确认信任此身份", current =>
                {
                    if (current.AuthorizationPathIsCurrent())
                    {
                        current.ProgressFileOperation(ProgressFileAction.TrustIdentity);
                    }
                });
                return true;
            }
            if (self.AuthorizationTask == AuthorizationViewTask.ImportBranch)
            {
                self.AddProgressControl(view, "导入为独立分支", current => current.BeginTaskImport());
            }
            else if (self.AuthorizationTask == AuthorizationViewTask.ContinueRestaurant)
            {
                if (joined && !room.Current.HasRestaurant && room.Current.CanControlRoom && !string.IsNullOrEmpty(room.Current.VerifiedProfileId))
                {
                    self.AddProgressControl(view, "发布此授权提案", current =>
                    {
                        if (current.AuthorizationPathIsCurrent())
                        {
                            current.LoadProgressAuthorization(current.InspectedAuthorization);
                        }
                    });
                }
                else
                {
                    view.SettlementText.text += "\n等待已验证身份的房主在空闲房间载入；本次未开局。";
                }
            }
            return true;

            void AddTask(string caption, AuthorizationViewTask task) => self.AddProgressControl(view, caption, current => current.NavigateAuthorizationTask(task), true);
        }

        private static void RenderAuthorizationExportTask(this ProductEntryPresentation self, MonoUIGridKitchen view, bool joined)
        {
            var hostBytes = ((ClientSessionComponent)self.Root().GetComponent<ProductRuntimeComponent>().Client)?.GetComponent<ProductRoomClient>()?.Current.HostIdentity;
            var host = hostBytes == null ? null : OfflineProfiles.DecodeIdentity(hostBytes);
            AddPurpose("导出独立分支许可", AuthorizationExportPurpose.IndependentBranch);
            AddPurpose("导出房间只读许可", AuthorizationExportPurpose.RoomReadRun);
            AddPurpose("导出替换许可", AuthorizationExportPurpose.Replacement);
            view.SettlementText.text = "导出给朋友\n先选用途，再选自己的记录；文件只在明确导出时生成。\n当前用途：" + (self.AuthorizationExport switch
            {
                AuthorizationExportPurpose.RoomReadRun => "房间只读运行；需加入目标房间并核对房主公共身份",
                AuthorizationExportPurpose.Replacement => "有损替换；需核对接收者公共身份，目标修订仍要单独确认",
                _ => "独立分支；朋友导入后归其所有，原记录保留"
            });
            if (self.AuthorizationOutputPath != null)
            {
                view.SettlementText.text += "\n刚生成的文件：\n" + System.IO.Path.GetFileName(self.AuthorizationOutputPath) +
                    "\n完整路径见输入框。\n只生成了文件，未替朋友导入或改写其进度。";
            }
            self.AddProgressControl(view, "本机成功记录", current => current.BrowseProgressRecords());
            self.AddProgressControl(view, "导出身份文件", current => current.ProgressFileOperation(ProgressFileAction.ExportIdentity));
            if (self.AuthorizationExport != AuthorizationExportPurpose.IndependentBranch)
            {
                self.AddProgressControl(view, "查看接收者身份", current => current.ProgressFileOperation(ProgressFileAction.Inspect));
                if (self.InspectedIdentity != null)
                {
                    view.SettlementText.text += "\n接收者：" + self.InspectedIdentity.DisplayName + " · 身份明细可展开";
                    if (!self.InspectedTrusted)
                    {
                        self.AddProgressControl(view, "确认信任此身份", current =>
                        {
                            if (current.AuthorizationPathIsCurrent())
                            {
                                current.ProgressFileOperation(ProgressFileAction.TrustIdentity);
                            }
                        });
                    }
                }
            }
            foreach (var header in self.ProgressHeaders)
            {
                string slot = header.SlotId;
                string label = "记录 " + slot.Substring(0, 8) + " · 修订 " + header.Revision;
                if (self.AuthorizationExport == AuthorizationExportPurpose.IndependentBranch)
                {
                    self.AddProgressControl(view, "导出分支许可 " + label, current => current.ExportProgressGrant(SaveAuthorizationAction.ExportImport, slot), true);
                }
                else if (self.InspectedIdentity != null && self.InspectedTrusted && self.InspectedAuthorization == null)
                {
                    if (self.AuthorizationExport == AuthorizationExportPurpose.Replacement)
                    {
                        self.AddProgressControl(view, "导出替换许可 " + label, current => current.ExportProgressGrant(SaveAuthorizationAction.ExportReplace, slot), true);
                    }
                    else if (joined && host != null && self.InspectedIdentity.ProfileId == host.ProfileId && self.InspectedIdentity.KeyId == host.KeyId)
                    {
                        self.AddProgressControl(view, "授权导出 " + label, current => current.ProgressFileOperation(ProgressFileAction.ExportReadRun, slot), true);
                    }
                }
            }
            if (self.AuthorizationExport == AuthorizationExportPurpose.RoomReadRun && self.InspectedIdentity != null && self.InspectedTrusted)
            {
                if (host == null || host.ProfileId != self.InspectedIdentity.ProfileId || host.KeyId != self.InspectedIdentity.KeyId)
                {
                    view.SettlementText.text += "\n所查身份不是当前已验证房主，请重新检查房主的公共身份文件。";
                }
            }
            void AddPurpose(string caption, AuthorizationExportPurpose purpose) => self.AddProgressControl(view, caption, current =>
            {
                ((UIGridKitchen)current.Hud)?.View.ClearTargets();
                current.AuthorizationExport = purpose; current.InspectedIdentity = null; current.InspectedAuthorization = null;
                current.InspectedAuthorizationKind = SaveAuthorizationKind.None; current.InspectedTrusted = false;
                current.AuthorizationIdentityExpanded = false; current.AuthorizationInspectedPath = null;
                current.AuthorizationOutputPath = null;
                current.ProgressPath = current.Failure = null; current.RenderProductEntry();
            }, true);
        }
    }
}
