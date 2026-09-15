namespace Hearthkeeper;

internal sealed class ContainerSettings
{
	internal ReserveMode Reserve;

	internal int CustomReserve;

	internal bool ManualLock;

	internal bool AcceptStorage;

	internal bool LivestockFeed;

	internal int AmountFor(ItemDrop.ItemData item)
	{
		switch (Reserve)
		{
		case ReserveMode.OneItem:
			return 1;
		case ReserveMode.OneStack:
			if (item == null || item.m_shared == null)
			{
				return 1;
			}
			return item.m_shared.m_maxStackSize;
		case ReserveMode.Custom:
			if (CustomReserve >= 0)
			{
				return CustomReserve;
			}
			return 0;
		default:
			return 0;
		}
	}
}
