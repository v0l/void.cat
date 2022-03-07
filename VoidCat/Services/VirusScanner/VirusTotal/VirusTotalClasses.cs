using Newtonsoft.Json;
// ReSharper disable InconsistentNaming
#pragma warning disable CS8618

namespace VoidCat.Services.VirusScanner.VirusTotal;

public class LastAnalysisStats
{
    [JsonProperty("confirmed-timeout")] 
    public int ConfirmedTimeout { get; set; }

    [JsonProperty("failure")] 
    public int Failure { get; set; }

    [JsonProperty("harmless")] 
    public int Harmless { get; set; }

    [JsonProperty("malicious")] 
    public int Malicious { get; set; }

    [JsonProperty("suspicious")] 
    public int Suspicious { get; set; }

    [JsonProperty("timeout")] 
    public int Timeout { get; set; }

    [JsonProperty("type-unsupported")] 
    public int TypeUnsupported { get; set; }

    [JsonProperty("undetected")] 
    public int Undetected { get; set; }
}

public class TotalVotes
{
    [JsonProperty("harmless")] 
    public int Harmless { get; set; }

    [JsonProperty("malicious")] 
    public int Malicious { get; set; }
}

public class Attributes
{
    [JsonProperty("capabilities_tags")] 
    public List<string> CapabilitiesTags { get; set; }

    [JsonProperty("creation_date")] 
    public int CreationDate { get; set; }

    [JsonProperty("downloadable")] 
    public bool Downloadable { get; set; }

    [JsonProperty("first_submission_date")]
    public int FirstSubmissionDate { get; set; }

    [JsonProperty("last_analysis_date")] 
    public int LastAnalysisDate { get; set; }

    [JsonProperty("last_analysis_stats")] 
    public LastAnalysisStats LastAnalysisStats { get; set; }

    [JsonProperty("last_modification_date")]
    public int LastModificationDate { get; set; }

    [JsonProperty("last_submission_date")] 
    public int LastSubmissionDate { get; set; }

    [JsonProperty("md5")] 
    public string Md5 { get; set; }

    [JsonProperty("meaningful_name")] 
    public string MeaningfulName { get; set; }

    [JsonProperty("names")] 
    public List<string> Names { get; set; }

    [JsonProperty("reputation")] 
    public int Reputation { get; set; }

    [JsonProperty("sha1")] 
    public string Sha1 { get; set; }

    [JsonProperty("sha256")] 
    public string Sha256 { get; set; }

    [JsonProperty("size")] 
    public int Size { get; set; }

    [JsonProperty("tags")] 
    public List<string> Tags { get; set; }

    [JsonProperty("times_submitted")] 
    public int TimesSubmitted { get; set; }

    [JsonProperty("total_votes")] 
    public TotalVotes TotalVotes { get; set; }

    [JsonProperty("type_description")] 
    public string TypeDescription { get; set; }

    [JsonProperty("type_tag")] 
    public string TypeTag { get; set; }

    [JsonProperty("unique_sources")] 
    public int UniqueSources { get; set; }

    [JsonProperty("vhash")] 
    public string Vhash { get; set; }
}

public class Links
{
    [JsonProperty("self")] 
    public string Self { get; set; }
}

public class File
{
    [JsonProperty("attributes")] 
    public Attributes Attributes { get; set; }

    [JsonProperty("id")] 
    public string Id { get; set; }

    [JsonProperty("links")] 
    public Links Links { get; set; }

    [JsonProperty("type")] 
    public string Type { get; set; }
}

public class Error
{
    [JsonProperty("code")]
    public string Code { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}

// ReSharper disable once InconsistentNaming
public class VTResponse<T>
{
    [JsonProperty("data")] 
    public T Data { get; set; }
    
    [JsonProperty("error")]
    public Error Error { get; set; }
}

public class VTException : Exception
{
    public VTException(Error error)
    {
        ErrorCode = Enum.TryParse<VTErrorCodes>(error.Code, out var c) ? c : VTErrorCodes.UnknownError;
        Message = error.Message;
    }

    public VTErrorCodes ErrorCode { get; }
    
    public string Message { get; }
}

public enum VTErrorCodes
{
    UnknownError,
    BadRequestError,
    InvalidArgumentError,
    NotAvailableYet,
    UnselectiveContentQueryError,
    UnsupportedContentQueryError,
    AuthenticationRequiredError,
    UserNotActiveError,
    WrongCredentialsError,
    ForbiddenError,
    NotFoundError,
    AlreadyExistsError,
    FailedDependencyError,
    QuotaExceededError,
    TooManyRequestsError,
    TransientError,
    DeadlineExceededError
}