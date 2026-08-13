using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectAtmaca.Domain.Common;

namespace ProjectAtmaca.Domain.Entities;

public sealed class Country : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Iso2Code { get; private set; } = string.Empty;

    public string Iso3Code { get; private set; } = string.Empty;

    public string PhoneCode { get; private set; } = string.Empty;

    public string CurrencyCode { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;
}
