#if !DOTNET
using System;
using System.IO;
using System.Linq;

namespace ET
{
    [FriendOf(typeof(SoloSaveRepository))]
    public static class ProgressClaimFiles
    {
        public static string PersistTicket(SoloSaveRepository repository, byte[] document, string expectedMatch, OfflineProfileIdentity expectedHost = null)
        {
            var verified = ProgressClaimCodec.Ticket(document, repository);
            RequireHost(verified.Authority.Host, expectedHost);
            if (verified.Success.MatchId != expectedMatch)
            {
                throw new InvalidOperationException("WrongMatch");
            }
            Publish(PathFor(repository, "Tickets", verified.Ticket.TicketId, ".ccticket"), document);
            Changed(repository);
            return verified.Ticket.TicketId;
        }

        public static string PersistClose(SoloSaveRepository repository, byte[] document, string expectedMatch, OfflineProfileIdentity expectedHost = null)
        {
            var value = ProgressClaimCodec.Close(document, repository);
            var host = ProgressClaimCodec.Authority(value.Authority, repository).Host;
            RequireHost(host, expectedHost);
            if (value.MatchId != expectedMatch)
            {
                throw new InvalidOperationException("WrongMatch");
            }
            string path = PathFor(repository, "Closures", value.MatchId, ".ccclose");
            if (File.Exists(path))
            {
                var prior = ProgressClaimCodec.Close(OfflineProfiles.ReadLimited(path, OfflineProfiles.MaxDocumentBytes), repository);
                RequireHost(ProgressClaimCodec.Authority(prior.Authority, repository).Host, host);
                if (prior.MatchId != value.MatchId || prior.ProfileId != value.ProfileId)
                {
                    throw new InvalidOperationException("InvalidCloseReceipt");
                }
            }
            else
            {
                Publish(path, document);
            }
            Changed(repository);
            return value.ReceiptId;
        }

        public static ProgressCommitReceipt Claim(SoloSaveRepository repository, string ticketId)
        {
            var verified = Read(repository, ticketId);
            var existing = ProgressReceiptIndex.Find(repository, ProgressCommitKind.Claim, verified.Success.SaveResultId, repository.ProfileId);
            if (existing != null)
            {
                return existing;
            }
            EnsureOpen(repository, verified);
            var receipts = ProgressReceiptIndex.Read(repository);
            var receipt = new ProgressCommitReceipt
            {
                Kind = ProgressCommitKind.Claim, ResultId = verified.Success.SaveResultId, ConsumerProfileId = repository.ProfileId,
                ProfileId = repository.ProfileId, SlotId = verified.Ticket.TicketId, Revision = 1
            };
            ProgressReceiptIndex.Add(receipts, receipt);
            var target = ProgressClaimCodec.Header(verified.Success, verified.Authority, repository.ProfileId, receipt.SlotId, ProgressReceiptIndex.Values(receipts));
            // 载荷与消费键都在同一ccsave中；没有后置标记才能算成功的窗口。
            repository.CommitWithReceipt(target, verified.Payload);
            Changed(repository);
            return ProgressReceiptValues.Copy(receipt);
        }

        public static ProgressTicketSummary[] Browse(SoloSaveRepository repository)
        {
            string directory = Path.Combine(repository.DirectoryPath, "Tickets");
            if (!Directory.Exists(directory))
            {
                return Array.Empty<ProgressTicketSummary>();
            }
            return Directory.EnumerateFiles(directory, "*.ccticket").Select(path =>
            {
                string id = Path.GetFileNameWithoutExtension(path);
                var verified = Read(repository, id);
                var claimed = ProgressReceiptIndex.Find(repository, ProgressCommitKind.Claim, verified.Success.SaveResultId, repository.ProfileId);
                return new ProgressTicketSummary
                {
                    TicketId = id, SaveResultId = verified.Success.SaveResultId, MatchId = verified.Success.MatchId,
                    CompletedLevelId = verified.Success.CompletedLevelId, Score = verified.Success.Score, Stars = verified.Success.Stars,
                    State = claimed != null ? ProgressReceiptState.Claimed : IsClosed(repository, verified.Success.MatchId, verified.Authority.Host) ? ProgressReceiptState.Closed : ProgressReceiptState.Available,
                    SlotId = claimed?.SlotId
                };
            }).OrderBy(summary => summary.CompletedLevelId).ThenBy(summary => summary.TicketId, StringComparer.Ordinal).ToArray();
        }

        public static VerifiedProgressTicket Read(SoloSaveRepository repository, string ticketId)
        {
            var value = ProgressClaimCodec.Ticket(OfflineProfiles.ReadLimited(PathFor(repository, "Tickets", ticketId, ".ccticket"), OfflineProfiles.MaxDocumentBytes), repository);
            if (value.Ticket.TicketId != ticketId)
            {
                throw new InvalidOperationException("InvalidClaimIdentity");
            }
            return value;
        }

        public static void EnsureOpen(SoloSaveRepository repository, VerifiedProgressTicket verified)
        {
            if (IsClosed(repository, verified.Success.MatchId, verified.Authority.Host))
            {
                throw new InvalidOperationException("ClaimClosed");
            }
        }

        private static bool IsClosed(SoloSaveRepository repository, string match, OfflineProfileIdentity expectedHost)
        {
            string path = PathFor(repository, "Closures", match, ".ccclose");
            if (!File.Exists(path))
            {
                return false;
            }
            var close = ProgressClaimCodec.Close(OfflineProfiles.ReadLimited(path, OfflineProfiles.MaxDocumentBytes), repository);
            RequireHost(ProgressClaimCodec.Authority(close.Authority, repository).Host, expectedHost);
            return close.MatchId == match;
        }

        private static void RequireHost(OfflineProfileIdentity actual, OfflineProfileIdentity expected)
        {
            if (expected != null && (actual.ProfileId != expected.ProfileId || actual.KeyId != expected.KeyId))
            {
                throw new InvalidOperationException("WrongMatchHost");
            }
        }

        private static string PathFor(SoloSaveRepository repository, string folder, string id, string extension)
        {
            if (!SoloSaveValidation.IsIdentifier(id))
            {
                throw new InvalidOperationException("InvalidClaimIdentity");
            }
            return Path.Combine(repository.DirectoryPath, folder, id + extension);
        }

        private static void Publish(string path, byte[] bytes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path))
            {
                if (!OfflineProfiles.ReadLimited(path, OfflineProfiles.MaxDocumentBytes).SequenceEqual(bytes))
                {
                    throw new InvalidOperationException("ClaimDocumentConflict");
                }
                return;
            }
            string temporary = Path.Combine(Path.GetDirectoryName(path), ".progress-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private static void Changed(SoloSaveRepository repository) => EventSystem.Instance.Publish((Scene)repository.IScene, new ProgressInboxChanged());
    }
}
#endif
