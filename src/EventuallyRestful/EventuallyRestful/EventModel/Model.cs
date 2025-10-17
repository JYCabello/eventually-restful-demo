using DeFuncto;
using EventuallyRestful.Mail;
using Nvx.ConsistentAPI;

namespace EventuallyRestful;

public record EmailSendId(Guid Value) : StrongId
{
  public override string StreamId()
  {
    return Value.ToString();
  }

  public override string ToString()
  {
    return StreamId();
  }
}

public partial record EmailSendEntity(Guid Id, string To, string RecipientName, string About)
  : EventModelEntity<EmailSendEntity>,
    Folds<EmailSendRequested, EmailSendEntity>
{
  public const string StreamPrefix = "email-send-entity-";

  public string GetStreamName()
  {
    return GetStreamName(Id);
  }

  public ValueTask<EmailSendEntity> Fold(EmailSendRequested evt, EventMetadata metadata, RevisionFetcher fetcher)
  {
    return ValueTask.FromResult(this with
    {
      To = evt.To,
      RecipientName = evt.RecipientName,
      About = evt.About
    });
  }

  public static string GetStreamName(Guid id)
  {
    return $"{StreamPrefix}{id}";
  }

  public static EmailSendEntity Defaulted(EmailSendId id)
  {
    return new EmailSendEntity(
      id.Value,
      string.Empty,
      string.Empty,
      string.Empty);
  }
}

public record EmailSendRequested(Guid Id, string To, string RecipientName, string About) : EventModelEvent
{
  public string GetStreamName()
  {
    return EmailSendEntity.GetStreamName(Id);
  }

  public StrongId GetEntityId()
  {
    return new EmailSendId(Id);
  }
}

public record RequestEmailSend(string To, string RecipientName, string About) : EventModelCommand<EmailSendEntity>
{
  public Result<EventInsertion, ApiError> Decide(Option<EmailSendEntity> entity, Option<UserSecurity> user,
    FileUpload[] files)
  {
    return new CreateStream(new EmailSendRequested(Guid.NewGuid(), To, RecipientName, About));
  }

  public Option<StrongId> TryGetEntityId(Option<UserSecurity> user)
  {
    return None;
  }
}

public record EmailSendTaskData(string To, string RecipientName, string About) : TodoData
{
  public static readonly TodoTaskDefinition Definition =
    new TodoTaskDefinition<EmailSendTaskData, EmailSendEntity, EmailSendRequested, EmailSendId>
    {
      Action = async (data, entity, _, _, _) =>
      {
        await SendGridContract.SendEmail(data.To, data.About, data.RecipientName);
        return TodoOutcome.Done;
      },
      Originator = (requested, _, _) => new EmailSendTaskData(requested.To, requested.RecipientName, requested.About),
      SourcePrefix = EmailSendEntity.StreamPrefix,
      Type = "send an email"
    };
}

public static class Model
{
  public static EventModel Get()
  {
    return new EventModel
    {
      Entities =
      [
        new EntityDefinition<EmailSendEntity, EmailSendId>
        {
          Defaulter = EmailSendEntity.Defaulted,
          StreamPrefix = EmailSendEntity.StreamPrefix
        }
      ],
      Commands =
      [
        new CommandDefinition<RequestEmailSend, EmailSendEntity>
        {
          AreaTag = "Marketing"
        }
      ],
      Tasks = [EmailSendTaskData.Definition]
    };
  }
}
