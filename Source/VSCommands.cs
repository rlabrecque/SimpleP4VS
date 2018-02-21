// Copyright 2018 Riley Labrecque

namespace SimpleP4VS
{
    using System;

    public static class PackageGuids
    {
        public const string guidSimpleP4VSPackageString = "c8d6b77d-e87a-40fc-9449-ac12bbd057ba";
        public static readonly Guid guidSimpleP4VSPackage = new Guid(guidSimpleP4VSPackageString);

        public const string guidCheckoutCommandSetString = "e96a66aa-6fc0-4426-8b97-2c140322fbe9";
        public static readonly Guid guidCheckoutCommandSet = new Guid(guidCheckoutCommandSetString);
    }

    public static class PackageIds
    {
        public const int CheckoutCommandId = 0x0100;
    }
}
