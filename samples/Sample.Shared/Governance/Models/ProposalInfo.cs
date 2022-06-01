namespace Sample.Shared.Governance.Models
{
	public class ProposalInfo
	{
		public NeuronId? Id { get; set; }
		
		public int Status { get; set; }
		
		public int Topic { get; set; }
		
		public GovernanceError? FailureReason { get; set; }
		
		public List<BallotsInfo> Ballots { get; set; }
		
		public ulong ProposalTimestampSeconds { get; set; }
		
		public ulong RewardEventRound { get; set; }
		
		public ulong? DeadlineTimestampSeconds { get; set; }
		
		public ulong FailedTimestampSeconds { get; set; }
		
		public ulong RejectCostE8s { get; set; }
		
		public Tally? LatestTally { get; set; }
		
		public int RewardStatus { get; set; }
		
		public ulong DecidedTimestampSeconds { get; set; }
		
		public Proposal? Proposal { get; set; }
		
		public NeuronId? Proposer { get; set; }
		
		public ulong ExecutedTimestampSeconds { get; set; }
		
		public class BallotsInfo
		{
			public ulong F0 { get; set; }
			
			public Ballot F1 { get; set; }
			
		}
	}
}
