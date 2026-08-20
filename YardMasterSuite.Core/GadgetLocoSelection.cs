namespace YardMasterSuite.Core
{
    /// <summary>
    /// Fluids / load / motors follow the usable loco (look-at wins). Last boarded
    /// is not the tank source when a different unit is in the crosshair.
    /// </summary>
    public static class GadgetLocoSelection
    {
        public static int ResolveInstanceId(int usableLocoId, int lastLocoId) =>
            usableLocoId != 0 ? usableLocoId : lastLocoId;
    }
}
