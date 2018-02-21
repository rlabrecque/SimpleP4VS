// Copyright 2018 Riley Labrecque

namespace SimpleP4VS
{
    using System;

    public static class PackageGuids
    {
        public const string guidSimpleP4VSPackageString = "c8d6b77d-e87a-40fc-9449-ac12bbd057ba";
        public static readonly Guid guidSimpleP4VSPackage = new Guid(guidSimpleP4VSPackageString);

        public const string guidCheckoutCommandSolutionViewSetString = "e96a66aa-6fc0-4426-8b97-2c140322fbe9";
        public static readonly Guid guidCheckoutCommandSolutionViewSet = new Guid(guidCheckoutCommandSolutionViewSetString);

        public const string guidCheckoutCommandActiveDocumentSetString = "f61b530a-73b2-4e07-b6e1-35aeb8dc27c2";
        public static readonly Guid guidCheckoutCommandActiveDocumentSet = new Guid(guidCheckoutCommandActiveDocumentSetString);
    }

    public static class PackageIds
    {
        public const int CheckoutCommandSolutionViewId = 0x0100;
        public const int CheckoutCommandActiveDocumentId = 0x0101;
    }
}
