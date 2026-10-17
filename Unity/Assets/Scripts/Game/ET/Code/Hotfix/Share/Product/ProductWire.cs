#if !DOTNET
using System;
using System.Text;
using Cysharp.Threading.Tasks;

namespace ET
{
    [Invoke((long)SceneType.ProductHost)]
    [FriendOf(typeof(ProductPeerBinding))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    public sealed class ProductHostOnRead : AInvokeHandler<NetComponentOnRead>
    {
        public override void Handle(NetComponentOnRead args)
        {
            Session session = args.Session;
            if (args.Message is not (ProductJoinRequest or ProductResumeRequest or ProductProbeRequest or ProductLeaveRequest or ProductHeartbeatRequest or GridCommandRequest or LevelCommandRequest or StationLayoutRequest or ChooseBuffRequest or CompanionPreferenceRequest or OrderIntentRequest or TargetPingRequest or SupplyRequestRequest or ChooseRenovationRequest or ServeOrderRequest or FrontWorkRequest or SupplyCommandRequest or ProductRoomControlRequest or ProductProfileRequest or CooperativeBaselineRequest or ProgressAckRequest or ProgressCloseMatchRequest))
            {
                (args.Message as MessageObject)?.Dispose();
                session.Error = ProductError.InvalidRequest;
                session.Dispose();
                return;
            }
            ProductPeerBinding binding = session.GetComponent<ProductPeerBinding>();
            if (binding == null && args.Message is ProductJoinRequest or ProductResumeRequest)
            {
                HostSessionComponent authority = ((Scene)session.IScene).GetComponent<HostSessionComponent>();
                HostPeer peer = ProductAuthority.CreatePeer(authority);
                peer.AddComponent<RemoteDelivery, Session>(session);
                binding = session.AddComponent<ProductPeerBinding, HostPeer>(peer);
            }
            HandleRequest(session, binding, (MessageObject)args.Message).Forget();
        }

        private static async UniTaskVoid HandleRequest(Session session, ProductPeerBinding binding, MessageObject requestObject)
        {
            IRequest wire = (IRequest)requestObject;
            int wireRpcId = wire.RpcId;
            EntityRef<Session> sessionRef = session;
            HostPeer peer = binding?.Peer ?? default;
            EntityRef<HostPeer> peerRef = peer;
            long generation = binding?.Generation ?? 0;
            bool entered = peer != null && peer.Generation == generation && peer.BeginEntry();
            MessageObject responseObject = null;
            try
            {
                if (!entered || wireRpcId <= 0)
                {
                    responseObject = (MessageObject)MessageHelper.CreateResponse(wire.GetType(), wireRpcId, ProductError.PeerClosed);
                }
                else if (wire is ProductProbeRequest invalidProbe &&
                    (string.IsNullOrEmpty(invalidProbe.MatchId) || invalidProbe.Sequence <= 0 || invalidProbe.Payload == null ||
                    Encoding.UTF8.GetByteCount(invalidProbe.Payload) > ProductSessionRules.MaxPayloadBytes))
                {
                    responseObject = (MessageObject)MessageHelper.CreateResponse(wire.GetType(), wireRpcId, ProductError.InvalidRequest);
                }
                else if (wire is ProductLeaveRequest invalidLeave && string.IsNullOrEmpty(invalidLeave.MatchId))
                {
                    responseObject = (MessageObject)MessageHelper.CreateResponse(wire.GetType(), wireRpcId, ProductError.InvalidRequest);
                }
                else if (wire is GridCommandRequest soloCommand && !IsKitchenGridAction(soloCommand.Action))
                {
                    responseObject = ProductMessages.Create<GridCommandResponse>(message =>
                    {
                        message.Error = ProductError.InvalidRequest; message.Message = "UnsupportedGridAction";
                        message.CommandId = soloCommand.CommandId; message.Status = (int)GridResultStatus.Rejected;
                    });
                }
                else if (wire is ProductHeartbeatRequest heartbeat)
                {
                    HostSessionComponent authority = peer.Authority;
                    Match match = authority.Match;
                    int error = heartbeat.ProtocolVersion != ProductSessionRules.ProtocolVersion ? ProductError.IncompatibleProtocol
                        : peer.MemberId == 0 ? ProductError.NotJoined
                        : heartbeat.MatchId != match.MatchId ? ProductError.WrongMatch : 0;
                    responseObject = ProductMessages.Create<ProductHeartbeatResponse>(message => { message.Error = error; });
                }
                else
                {
                    // 独立对象进入本地 Call；其 RpcId 改写不会覆盖 wire 的原编号。
                    IRequest inner = wire switch
                    {
                        ProductJoinRequest join => ProductMessages.Create<ProductJoinRequest>(message => { message.ProtocolVersion = join.ProtocolVersion; }),
                        ProductResumeRequest resume => ProductMessages.Create<ProductResumeRequest>(message =>
                        {
                            message.ProtocolVersion = resume.ProtocolVersion; message.MatchId = resume.MatchId;
                            message.ResumeCredential = resume.ResumeCredential; message.AttemptId = resume.AttemptId;
                            message.PlatformId = resume.PlatformId;
                        }),
                        ProductProbeRequest probe => ProductMessages.Create<ProductProbeRequest>(message => { message.MatchId = probe.MatchId; message.Sequence = probe.Sequence; message.Payload = probe.Payload; }),
                        ProductLeaveRequest leave => ProductMessages.Create<ProductLeaveRequest>(message => { message.MatchId = leave.MatchId; }),
                        ProductRoomControlRequest room => ProductMessages.Create<ProductRoomControlRequest>(message =>
                        {
                            message.ProtocolVersion = room.ProtocolVersion;
                            message.MatchId = room.MatchId;
                            message.BindingGeneration = room.BindingGeneration;
                            message.CommandId = room.CommandId;
                            message.ExpectedRoomVersion = room.ExpectedRoomVersion;
                            message.Action = room.Action;
                            message.MenuVariant = room.MenuVariant;
                            message.ContentChapterId = room.ContentChapterId;
                            message.ExpectedProposalVersion = room.ExpectedProposalVersion;
                        }),
                        ProductProfileRequest profile => ProductMessages.Create<ProductProfileRequest>(message =>
                        {
                            message.ProtocolVersion = profile.ProtocolVersion;
                            message.MatchId = profile.MatchId;
                            message.BindingGeneration = profile.BindingGeneration;
                            message.Action = profile.Action;
                            message.Proof = profile.Proof == null ? null : (byte[])profile.Proof.Clone();
                        }),
                        CooperativeBaselineRequest baseline => ProductMessages.Create<CooperativeBaselineRequest>(message =>
                        {
                            message.ProtocolVersion = baseline.ProtocolVersion;
                            message.MatchId = baseline.MatchId;
                            message.BindingGeneration = baseline.BindingGeneration;
                            message.CommandId = baseline.CommandId;
                            message.ExpectedRoomVersion = baseline.ExpectedRoomVersion;
                            message.Authorization = baseline.Authorization == null ? null : (byte[])baseline.Authorization.Clone();
                        }),
                        ProgressAckRequest ack => ProductMessages.Create<ProgressAckRequest>(message =>
                        {
                            message.ProtocolVersion = ack.ProtocolVersion; message.MatchId = ack.MatchId; message.BindingGeneration = ack.BindingGeneration;
                            message.Kind = ack.Kind; message.Id = ack.Id;
                        }),
                        ProgressCloseMatchRequest close => ProductMessages.Create<ProgressCloseMatchRequest>(message =>
                        {
                            message.ProtocolVersion = close.ProtocolVersion; message.MatchId = close.MatchId; message.BindingGeneration = close.BindingGeneration;
                            message.ExpectedRoomVersion = close.ExpectedRoomVersion;
                        }),
                        GridCommandRequest grid => ProductMessages.Create<GridCommandRequest>(message =>
                        {
                            message.ProtocolVersion = grid.ProtocolVersion; message.MatchId = grid.MatchId; message.WorldId = grid.WorldId;
                            message.WorldGeneration = grid.WorldGeneration; message.BindingGeneration = grid.BindingGeneration;
                            message.CommandId = grid.CommandId; message.Action = grid.Action; message.X = grid.X; message.Y = grid.Y;
                            message.TargetId = grid.TargetId; message.ExpectedItemId = grid.ExpectedItemId;
                            message.ExpectedTargetItemId = grid.ExpectedTargetItemId; message.ExpectedProcessId = grid.ExpectedProcessId;
                            message.ExpectedOrderId = grid.ExpectedOrderId; message.RecipeId = grid.RecipeId;
                            message.LevelId = grid.LevelId; message.LevelGeneration = grid.LevelGeneration;
                            message.ExpectedContentIds.AddRange(grid.ExpectedContentIds);
                            message.ExpectedRemainingPortions = grid.ExpectedRemainingPortions; message.ExpectedLayoutVersion = grid.ExpectedLayoutVersion;
                        }),
                        LevelCommandRequest level => ProductMessages.Create<LevelCommandRequest>(message =>
                        {
                            message.ProtocolVersion = level.ProtocolVersion; message.MatchId = level.MatchId; message.WorldId = level.WorldId;
                            message.WorldGeneration = level.WorldGeneration; message.BindingGeneration = level.BindingGeneration;
                            message.LevelId = level.LevelId; message.LevelGeneration = level.LevelGeneration; message.CommandId = level.CommandId;
                            message.Action = level.Action; message.NextLevelConfigId = level.NextLevelConfigId;
                        }),
                        StationLayoutRequest layout => ProductMessages.Create<StationLayoutRequest>(message =>
                        {
                            message.ProtocolVersion = layout.ProtocolVersion; message.MatchId = layout.MatchId; message.WorldId = layout.WorldId;
                            message.WorldGeneration = layout.WorldGeneration; message.BindingGeneration = layout.BindingGeneration;
                            message.LevelId = layout.LevelId; message.LevelGeneration = layout.LevelGeneration; message.CommandId = layout.CommandId;
                            message.StationId = layout.StationId; message.ExpectedLayoutVersion = layout.ExpectedLayoutVersion;
                            message.X = layout.X; message.Y = layout.Y; message.Orientation = layout.Orientation;
                        }),
                        CompanionPreferenceRequest preference => ProductMessages.Create<CompanionPreferenceRequest>(message =>
                        {
                            message.ProtocolVersion = preference.ProtocolVersion; message.MatchId = preference.MatchId; message.WorldId = preference.WorldId;
                            message.WorldGeneration = preference.WorldGeneration; message.BindingGeneration = preference.BindingGeneration;
                            message.LevelId = preference.LevelId; message.LevelGeneration = preference.LevelGeneration;
                            message.CommandId = preference.CommandId; message.Preference = preference.Preference;
                        }),
                        OrderIntentRequest preference => ProductMessages.Create<OrderIntentRequest>(message =>
                        {
                            message.ProtocolVersion = preference.ProtocolVersion; message.MatchId = preference.MatchId; message.WorldId = preference.WorldId;
                            message.WorldGeneration = preference.WorldGeneration; message.BindingGeneration = preference.BindingGeneration;
                            message.LevelId = preference.LevelId; message.LevelGeneration = preference.LevelGeneration;
                            message.CommandId = preference.CommandId; message.Action = preference.Action; message.OrderId = preference.OrderId; message.CustomerId = preference.CustomerId;
                        }),
                        TargetPingRequest preference => ProductMessages.Create<TargetPingRequest>(message =>
                        {
                            message.ProtocolVersion = preference.ProtocolVersion; message.MatchId = preference.MatchId; message.WorldId = preference.WorldId;
                            message.WorldGeneration = preference.WorldGeneration; message.BindingGeneration = preference.BindingGeneration;
                            message.LevelId = preference.LevelId; message.LevelGeneration = preference.LevelGeneration;
                            message.CommandId = preference.CommandId; message.Action = preference.Action; message.TargetKind = preference.TargetKind; message.TargetId = preference.TargetId;
                        }),
                        SupplyRequestRequest preference => ProductMessages.Create<SupplyRequestRequest>(message =>
                        {
                            message.ProtocolVersion = preference.ProtocolVersion; message.MatchId = preference.MatchId; message.WorldId = preference.WorldId;
                            message.WorldGeneration = preference.WorldGeneration; message.BindingGeneration = preference.BindingGeneration;
                            message.LevelId = preference.LevelId; message.LevelGeneration = preference.LevelGeneration;
                            message.CommandId = preference.CommandId; message.Action = preference.Action; message.SourceId = preference.SourceId; message.ExpectedRequestId = preference.ExpectedRequestId; message.ExpectedRevision = preference.ExpectedRevision;
                        }),
                        ChooseBuffRequest buff => ProductMessages.Create<ChooseBuffRequest>(message =>
                        {
                            message.ProtocolVersion = buff.ProtocolVersion; message.MatchId = buff.MatchId; message.WorldId = buff.WorldId;
                            message.WorldGeneration = buff.WorldGeneration; message.BindingGeneration = buff.BindingGeneration;
                            message.LevelId = buff.LevelId; message.LevelGeneration = buff.LevelGeneration; message.CommandId = buff.CommandId;
                            message.DrawVersion = buff.DrawVersion; message.ChoiceDefId = buff.ChoiceDefId;
                        }),
                        ChooseRenovationRequest renovation => ProductMessages.Create<ChooseRenovationRequest>(message =>
                        {
                            message.ProtocolVersion = renovation.ProtocolVersion; message.MatchId = renovation.MatchId; message.WorldId = renovation.WorldId;
                            message.WorldGeneration = renovation.WorldGeneration; message.BindingGeneration = renovation.BindingGeneration;
                            message.LevelId = renovation.LevelId; message.LevelGeneration = renovation.LevelGeneration; message.CommandId = renovation.CommandId;
                            message.ChoiceVersion = renovation.ChoiceVersion; message.DefinitionId = renovation.DefinitionId;
                            message.TargetStationId = renovation.TargetStationId; message.ExpectedUpgradeLevel = renovation.ExpectedUpgradeLevel;
                        }),
                        ServeOrderRequest serve => ProductMessages.Create<ServeOrderRequest>(message =>
                        {
                            message.ProtocolVersion = serve.ProtocolVersion; message.MatchId = serve.MatchId; message.WorldId = serve.WorldId;
                            message.WorldGeneration = serve.WorldGeneration; message.BindingGeneration = serve.BindingGeneration;
                            message.LevelId = serve.LevelId; message.LevelGeneration = serve.LevelGeneration; message.CommandId = serve.CommandId;
                            message.OrderId = serve.OrderId; message.CustomerId = serve.CustomerId; message.TableStationId = serve.TableStationId; message.ItemId = serve.ItemId;
                        }),
                        FrontWorkRequest front => ProductMessages.Create<FrontWorkRequest>(message =>
                        {
                            message.ProtocolVersion = front.ProtocolVersion; message.MatchId = front.MatchId; message.WorldId = front.WorldId;
                            message.WorldGeneration = front.WorldGeneration; message.BindingGeneration = front.BindingGeneration;
                            message.LevelId = front.LevelId; message.LevelGeneration = front.LevelGeneration; message.CommandId = front.CommandId;
                            message.Action = front.Action; message.WorkId = front.WorkId; message.ClaimSerial = front.ClaimSerial;
                        }),
                        SupplyCommandRequest supply => ProductMessages.Create<SupplyCommandRequest>(message =>
                        {
                            message.ProtocolVersion = supply.ProtocolVersion; message.MatchId = supply.MatchId; message.WorldId = supply.WorldId;
                            message.WorldGeneration = supply.WorldGeneration; message.BindingGeneration = supply.BindingGeneration;
                            message.LevelId = supply.LevelId; message.LevelGeneration = supply.LevelGeneration; message.CommandId = supply.CommandId;
                            message.Action = supply.Action; message.SourceId = supply.SourceId; message.Quantity = supply.Quantity;
                            message.StationId = supply.StationId; message.ShipmentId = supply.ShipmentId; message.PackageId = supply.PackageId;
                            message.ContentItemId = supply.ContentItemId; message.LayoutVersion = supply.LayoutVersion;
                            message.CooperativeRequestId = supply.CooperativeRequestId; message.CooperativeRevision = supply.CooperativeRevision;
                        }),
                        _ => throw new InvalidOperationException("Product wire whitelist mismatch")
                    };
                    using var result = (MessageObject)await peer.Root().GetComponent<ProcessInnerSender>().Call(peer.GetActorId(), inner, false);
                    responseObject = CopyResponse(result);
                }
                session = sessionRef;
                peer = peerRef;
                if (responseObject is ProductResumeResponse { Error: 0 } resumed)
                {
                    generation = resumed.BindingGeneration;
                }
                if (session == null || (entered && (peer == null || peer.Generation != generation)))
                {
                    return;
                }
                IResponse response = (IResponse)responseObject;
                response.RpcId = wireRpcId;
                bool committedLeave = response is ProductLeaveResponse { Error: 0, Committed: true };
                if (entered && !peer.EntryOpen && !committedLeave)
                {
                    return;
                }
                if (response is ProductJoinResponse { Error: 0 } or ProductResumeResponse { Error: 0 })
                {
                    session.RemoveComponent<SessionAcceptTimeoutComponent>();
                }
                session.Send((IMessage)responseObject);
                if (committedLeave || response is ProductJoinResponse { Error: not 0 } or ProductResumeResponse { Error: not 0 })
                {
                    // KCP Send 入队后由后续 Update 发出，不能立即 Dispose 丢弃该响应。
                    await session.Root().GetComponent<TimerComponent>().WaitAsync(ProductSessionRules.ResponseCloseGrace);
                    session = sessionRef;
                    session?.Dispose();
                }
            }
            catch (Exception e)
            {
                session = sessionRef;
                if (session != null)
                {
                    Log.Warning($"Product wire request failed: {e.Message}");
                    session.Error = ProductError.PeerClosed;
                    session.Dispose();
                }
            }
            finally
            {
                requestObject.Dispose();
                responseObject?.Dispose();
                peer = peerRef;
                if (entered && peer != null)
                {
                    peer.EndEntry();
                }
            }
        }

        private static bool IsKitchenGridAction(int action) => action is (int)GridAction.Move or (int)GridAction.TakeCounter or
            (int)GridAction.PlaceCounter or (int)GridAction.PickupDrop or (int)GridAction.AddToContainer or
            (int)GridAction.StartStationWork or (int)GridAction.StartHeldWork or (int)GridAction.StopWork or
            (int)GridAction.PourContents or (int)GridAction.StartAutoWork or (int)GridAction.PortionSoup or (int)GridAction.StartBake or (int)GridAction.StartFry or
            (int)GridAction.FillWater or (int)GridAction.DrainLiquid or (int)GridAction.DiscardSolid or (int)GridAction.ReceiveSupply or (int)GridAction.TakeSupplyPortion or
            (int)GridAction.StartJuice or (int)GridAction.PortionJuice or (int)GridAction.BindDrink or (int)GridAction.UnbindDrink or (int)GridAction.TakeContainerContent or (int)GridAction.StartAssembly or (int)GridAction.StartMix or
            (int)GridAction.PortionBatter or (int)GridAction.Unmold or (int)GridAction.StartBrewTea or (int)GridAction.StartBoilPearls or (int)GridAction.StartMixTea or (int)GridAction.PortionTeaBase or (int)GridAction.StartSteam or (int)GridAction.StartGrind or (int)GridAction.StartExtract or
            (int)GridAction.HeatMilk or (int)GridAction.FoamMilk or (int)GridAction.PortionDrinkBase;

        private static MessageObject CopyResponse(MessageObject result)
        {
            switch (result)
            {
                case ProductJoinResponse join:
                    var response = ProductMessages.Create<ProductJoinResponse>(message => { message.Error = join.Error; message.Message = join.Message; message.MatchId = join.MatchId; message.MemberId = join.MemberId; message.BindingGeneration = join.BindingGeneration; message.RosterVersion = join.RosterVersion; message.ResumeCredential = join.ResumeCredential; });
                    foreach (ProductRosterMember member in join.Members)
                    {
                        response.Members.Add(ProductMessages.Create<ProductRosterMember>(message => { message.MemberId = member.MemberId; message.Online = member.Online; }));
                    }
                    return response;
                case ProductResumeResponse resume:
                    var resumed = ProductMessages.Create<ProductResumeResponse>(message =>
                    {
                        message.Error = resume.Error; message.Message = resume.Message; message.MatchId = resume.MatchId;
                        message.MemberId = resume.MemberId; message.BindingGeneration = resume.BindingGeneration;
                        message.RosterVersion = resume.RosterVersion; message.AttemptId = resume.AttemptId;
                    });
                    foreach (ProductRosterMember member in resume.Members)
                    {
                        resumed.Members.Add(ProductMessages.Create<ProductRosterMember>(message => { message.MemberId = member.MemberId; message.Online = member.Online; }));
                    }
                    return resumed;
                case ProductProbeResponse probe:
                    return ProductMessages.Create<ProductProbeResponse>(message => { message.Error = probe.Error; message.Message = probe.Message; message.MemberId = probe.MemberId; message.Sequence = probe.Sequence; message.Payload = probe.Payload; });
                case ProductLeaveResponse leave:
                    return ProductMessages.Create<ProductLeaveResponse>(message => { message.Error = leave.Error; message.Message = leave.Message; message.Committed = leave.Committed; });
                case ProductRoomControlResponse room:
                    return ProductMessages.Create<ProductRoomControlResponse>(message =>
                    {
                        message.Error = room.Error;
                        message.Message = room.Message;
                        message.CommandId = room.CommandId;
                        message.Status = room.Status;
                    });
                case ProductProfileResponse profile:
                    return ProductMessages.Create<ProductProfileResponse>(message =>
                    {
                        message.Error = profile.Error;
                        message.Message = profile.Message;
                        message.Challenge = profile.Challenge;
                        message.ProfileId = profile.ProfileId;
                    });
                case CooperativeBaselineResponse baseline:
                    return ProductMessages.Create<CooperativeBaselineResponse>(message =>
                    {
                        message.Error = baseline.Error;
                        message.Message = baseline.Message;
                        message.CommandId = baseline.CommandId;
                        message.Status = baseline.Status;
                    });
                case ProgressAckResponse ack:
                    return ProductMessages.Create<ProgressAckResponse>(message => { message.Error = ack.Error; message.Message = ack.Message; });
                case ProgressCloseMatchResponse close:
                    return ProductMessages.Create<ProgressCloseMatchResponse>(message => { message.Error = close.Error; message.Message = close.Message; message.Status = close.Status; });
                case GridCommandResponse grid:
                    return ProductMessages.Create<GridCommandResponse>(message => { message.Error = grid.Error; message.Message = grid.Message; message.CommandId = grid.CommandId; message.Status = grid.Status; });
                case LevelCommandResponse level:
                    return ProductMessages.Create<LevelCommandResponse>(message => { message.Error = level.Error; message.Message = level.Message; message.CommandId = level.CommandId; message.Status = level.Status; });
                case StationLayoutResponse layout:
                    return ProductMessages.Create<StationLayoutResponse>(message => { message.Error = layout.Error; message.Message = layout.Message; message.CommandId = layout.CommandId; message.Status = layout.Status; });
                case CompanionPreferenceResponse preference:
                    return ProductMessages.Create<CompanionPreferenceResponse>(message => { message.Error = preference.Error; message.Message = preference.Message; message.CommandId = preference.CommandId; message.Status = preference.Status; });
                case OrderIntentResponse intent:
                    return ProductMessages.Create<OrderIntentResponse>(message => { message.Error = intent.Error; message.Message = intent.Message; message.CommandId = intent.CommandId; message.Status = intent.Status; });
                case TargetPingResponse intent:
                    return ProductMessages.Create<TargetPingResponse>(message => { message.Error = intent.Error; message.Message = intent.Message; message.CommandId = intent.CommandId; message.Status = intent.Status; });
                case SupplyRequestResponse intent:
                    return ProductMessages.Create<SupplyRequestResponse>(message => { message.Error = intent.Error; message.Message = intent.Message; message.CommandId = intent.CommandId; message.Status = intent.Status; });
                case ChooseBuffResponse buff:
                    return ProductMessages.Create<ChooseBuffResponse>(message => { message.Error = buff.Error; message.Message = buff.Message; message.CommandId = buff.CommandId; message.Status = buff.Status; });
                case ChooseRenovationResponse renovation:
                    return ProductMessages.Create<ChooseRenovationResponse>(message => { message.Error = renovation.Error; message.Message = renovation.Message; message.CommandId = renovation.CommandId; message.Status = renovation.Status; });
                case ServeOrderResponse serve:
                    return ProductMessages.Create<ServeOrderResponse>(message => { message.Error = serve.Error; message.Message = serve.Message; message.CommandId = serve.CommandId; message.Status = serve.Status; });
                case FrontWorkResponse front:
                    return ProductMessages.Create<FrontWorkResponse>(message => { message.Error = front.Error; message.Message = front.Message; message.CommandId = front.CommandId; message.Status = front.Status; });
                case SupplyCommandResponse supply:
                    return ProductMessages.Create<SupplyCommandResponse>(message => { message.Error = supply.Error; message.Message = supply.Message; message.CommandId = supply.CommandId; message.Status = supply.Status; });
                default:
                    throw new InvalidOperationException("Unexpected product actor response");
            }
        }
    }

    [Invoke((long)SceneType.ProductClient)]
    [FriendOf(typeof(ProductClientBinding))]
    [FriendOf(typeof(Session))]
    [FriendOf(typeof(ClientSessionComponent))]
    public sealed class ProductClientOnRead : AInvokeHandler<NetComponentOnRead>
    {
        public override void Handle(NetComponentOnRead args)
        {
            Session session = args.Session;
            ClientSessionComponent client = session.GetComponent<ProductClientBinding>()?.Client ?? default;
            MessageObject message = args.Message as MessageObject;
            if (client == null || message == null || (Session)client.Session != session)
            {
                message?.Dispose();
                return;
            }
            if (message is ProductJoinResponse or ProductResumeResponse or ProductProbeResponse or ProductLeaveResponse or ProductHeartbeatResponse or GridCommandResponse or LevelCommandResponse or StationLayoutResponse or ChooseBuffResponse or CompanionPreferenceResponse or OrderIntentResponse or TargetPingResponse or SupplyRequestResponse or ChooseRenovationResponse or ServeOrderResponse or FrontWorkResponse or SupplyCommandResponse or ProductRoomControlResponse or ProductProfileResponse or CooperativeBaselineResponse or ProgressAckResponse or ProgressCloseMatchResponse)
            {
                IResponse response = (IResponse)message;
                if (!session.requestCallbacks.TryGetValue(response.RpcId, out RpcInfo pending))
                {
                    message.Dispose();
                    return;
                }
                if (OpcodeType.Instance.GetResponseType(pending.RequestType) != message.GetType())
                {
                    message.Dispose();
                    session.Error = ProductError.InvalidRequest;
                    session.Dispose();
                    return;
                }
                session.OnResponse(response);
                return; // 有效响应交调用者消费。
            }
            if (message is ProductRosterSnapshot or ProductMatchClosed or GridWorldSnapshot or GridCommandResult or ProductCommandOutcome or KitchenWorkOutcome or ShipmentOutcome or FrontWorkOutcome or BuffPrepared or RenovationPrepared or LevelPhaseChanged or ProductRoomSnapshot or ProgressDelivery)
            {
                session.Root().GetComponent<ProcessInnerSender>().Send(client.GetActorId(), (IMessage)message);
                return;
            }
            message.Dispose();
            session.Error = ProductError.InvalidRequest;
            session.Dispose();
        }
    }
}
#endif
