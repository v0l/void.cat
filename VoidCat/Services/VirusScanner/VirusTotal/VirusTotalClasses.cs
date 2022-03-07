using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace VoidCat.Services.VirusScanner.VirusTotal;

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class AlertContext
{
    [JsonProperty("proto")] public string Proto { get; set; }

    [JsonProperty("src_ip")] public string SrcIp { get; set; }

    [JsonProperty("src_port")] public int SrcPort { get; set; }
}

public class CrowdsourcedIdsResult
{
    [JsonProperty("alert_context")] public List<AlertContext> AlertContext { get; set; }

    [JsonProperty("alert_severity")] public string AlertSeverity { get; set; }

    [JsonProperty("rule_category")] public string RuleCategory { get; set; }

    [JsonProperty("rule_id")] public string RuleId { get; set; }

    [JsonProperty("rule_msg")] public string RuleMsg { get; set; }

    [JsonProperty("rule_source")] public string RuleSource { get; set; }
}

public class CrowdsourcedIdsStats
{
    [JsonProperty("high")] public int High { get; set; }

    [JsonProperty("info")] public int Info { get; set; }

    [JsonProperty("low")] public int Low { get; set; }

    [JsonProperty("medium")] public int Medium { get; set; }
}

public class CrowdsourcedYaraResult
{
    [JsonProperty("description")] public string Description { get; set; }

    [JsonProperty("match_in_subfile")] public bool MatchInSubfile { get; set; }

    [JsonProperty("rule_name")] public string RuleName { get; set; }

    [JsonProperty("ruleset_id")] public string RulesetId { get; set; }

    [JsonProperty("ruleset_name")] public string RulesetName { get; set; }

    [JsonProperty("source")] public string Source { get; set; }
}

public class ALYac
{
    [JsonProperty("category")] public string Category { get; set; }

    [JsonProperty("engine_name")] public string EngineName { get; set; }

    [JsonProperty("engine_update")] public string EngineUpdate { get; set; }

    [JsonProperty("engine_version")] public string EngineVersion { get; set; }

    [JsonProperty("method")] public string Method { get; set; }

    [JsonProperty("result")] public string Result { get; set; }
}

public class APEX
{
    [JsonProperty("category")] public string Category { get; set; }

    [JsonProperty("engine_name")] public string EngineName { get; set; }

    [JsonProperty("engine_update")] public string EngineUpdate { get; set; }

    [JsonProperty("engine_version")] public string EngineVersion { get; set; }

    [JsonProperty("method")] public string Method { get; set; }

    [JsonProperty("result")] public string Result { get; set; }
}

public class AVG
{
    [JsonProperty("category")] public string Category { get; set; }

    [JsonProperty("engine_name")] public string EngineName { get; set; }

    [JsonProperty("engine_update")] public string EngineUpdate { get; set; }

    [JsonProperty("engine_version")] public string EngineVersion { get; set; }

    [JsonProperty("method")] public string Method { get; set; }

    [JsonProperty("result")] public string Result { get; set; }
}

public class Acronis
{
    [JsonProperty("category")] public string Category { get; set; }

    [JsonProperty("engine_name")] public string EngineName { get; set; }

    [JsonProperty("engine_update")] public string EngineUpdate { get; set; }

    [JsonProperty("engine_version")] public string EngineVersion { get; set; }

    [JsonProperty("method")] public string Method { get; set; }

    [JsonProperty("result")] public object Result { get; set; }
}

public class LastAnalysisResults
{
    [JsonProperty("ALYac")] public ALYac ALYac { get; set; }

    [JsonProperty("APEX")] public APEX APEX { get; set; }

    [JsonProperty("AVG")] public AVG AVG { get; set; }

    [JsonProperty("Acronis")] public Acronis Acronis { get; set; }
}

public class LastAnalysisStats
{
    [JsonProperty("confirmed-timeout")] public int ConfirmedTimeout { get; set; }

    [JsonProperty("failure")] public int Failure { get; set; }

    [JsonProperty("harmless")] public int Harmless { get; set; }

    [JsonProperty("malicious")] public int Malicious { get; set; }

    [JsonProperty("suspicious")] public int Suspicious { get; set; }

    [JsonProperty("timeout")] public int Timeout { get; set; }

    [JsonProperty("type-unsupported")] public int TypeUnsupported { get; set; }

    [JsonProperty("undetected")] public int Undetected { get; set; }
}

public class VirusTotalJujubox
{
    [JsonProperty("category")] public string Category { get; set; }

    [JsonProperty("confidence")] public int Confidence { get; set; }

    [JsonProperty("malware_classification")]
    public List<string> MalwareClassification { get; set; }

    [JsonProperty("malware_names")] public List<string> MalwareNames { get; set; }

    [JsonProperty("sandbox_name")] public string SandboxName { get; set; }
}

public class SandboxVerdicts
{
    [JsonProperty("VirusTotal Jujubox")] public VirusTotalJujubox VirusTotalJujubox { get; set; }
}

public class SigmaAnalysisStats
{
    [JsonProperty("critical")] public int Critical { get; set; }

    [JsonProperty("high")] public int High { get; set; }

    [JsonProperty("low")] public int Low { get; set; }

    [JsonProperty("medium")] public int Medium { get; set; }
}

public class SigmaIntegratedRuleSetGitHub
{
    [JsonProperty("critical")] public int Critical { get; set; }

    [JsonProperty("high")] public int High { get; set; }

    [JsonProperty("low")] public int Low { get; set; }

    [JsonProperty("medium")] public int Medium { get; set; }
}

public class SigmaAnalysisSummary
{
    [JsonProperty("Sigma Integrated Rule Set (GitHub)")]
    public SigmaIntegratedRuleSetGitHub SigmaIntegratedRuleSetGitHub { get; set; }
}

public class TotalVotes
{
    [JsonProperty("harmless")] public int Harmless { get; set; }

    [JsonProperty("malicious")] public int Malicious { get; set; }
}

public class Attributes
{
    [JsonProperty("capabilities_tags")] public List<string> CapabilitiesTags { get; set; }

    [JsonProperty("creation_date")] public int CreationDate { get; set; }

    [JsonProperty("crowdsourced_ids_results")]
    public List<CrowdsourcedIdsResult> CrowdsourcedIdsResults { get; set; }

    [JsonProperty("crowdsourced_ids_stats")]
    public CrowdsourcedIdsStats CrowdsourcedIdsStats { get; set; }

    [JsonProperty("crowdsourced_yara_results")]
    public List<CrowdsourcedYaraResult> CrowdsourcedYaraResults { get; set; }

    [JsonProperty("downloadable")] public bool Downloadable { get; set; }

    [JsonProperty("first_submission_date")]
    public int FirstSubmissionDate { get; set; }

    [JsonProperty("last_analysis_date")] public int LastAnalysisDate { get; set; }

    [JsonProperty("last_analysis_results")]
    public LastAnalysisResults LastAnalysisResults { get; set; }

    [JsonProperty("last_analysis_stats")] public LastAnalysisStats LastAnalysisStats { get; set; }

    [JsonProperty("last_modification_date")]
    public int LastModificationDate { get; set; }

    [JsonProperty("last_submission_date")] public int LastSubmissionDate { get; set; }

    [JsonProperty("md5")] public string Md5 { get; set; }

    [JsonProperty("meaningful_name")] public string MeaningfulName { get; set; }

    [JsonProperty("names")] public List<string> Names { get; set; }

    [JsonProperty("reputation")] public int Reputation { get; set; }

    [JsonProperty("sandbox_verdicts")] public SandboxVerdicts SandboxVerdicts { get; set; }

    [JsonProperty("sha1")] public string Sha1 { get; set; }

    [JsonProperty("sha256")] public string Sha256 { get; set; }

    [JsonProperty("sigma_analysis_stats")] public SigmaAnalysisStats SigmaAnalysisStats { get; set; }

    [JsonProperty("sigma_analysis_summary")]
    public SigmaAnalysisSummary SigmaAnalysisSummary { get; set; }

    [JsonProperty("size")] public int Size { get; set; }

    [JsonProperty("tags")] public List<string> Tags { get; set; }

    [JsonProperty("times_submitted")] public int TimesSubmitted { get; set; }

    [JsonProperty("total_votes")] public TotalVotes TotalVotes { get; set; }

    [JsonProperty("type_description")] public string TypeDescription { get; set; }

    [JsonProperty("type_tag")] public string TypeTag { get; set; }

    [JsonProperty("unique_sources")] public int UniqueSources { get; set; }

    [JsonProperty("vhash")] public string Vhash { get; set; }
}

public class Links
{
    [JsonProperty("self")] public string Self { get; set; }
}

public class File
{
    [JsonProperty("attributes")] public Attributes Attributes { get; set; }

    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("links")] public Links Links { get; set; }

    [JsonProperty("type")] public string Type { get; set; }
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
        Error = error;
    }

    protected VTException(SerializationInfo info, StreamingContext context, Error error) : base(info, context)
    {
        Error = error;
    }

    public VTException(string? message, Error error) : base(message)
    {
        Error = error;
    }

    public VTException(string? message, Exception? innerException, Error error) : base(message, innerException)
    {
        Error = error;
    }

    public Error Error { get; }
}