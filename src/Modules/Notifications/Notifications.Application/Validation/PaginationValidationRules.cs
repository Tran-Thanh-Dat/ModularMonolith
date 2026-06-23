using FluentValidation;

namespace Notifications.Application.Validation;

public static class PaginationValidationRules
{
    public static IRuleBuilderOptions<T, int> ValidPageIndex<T>(this IRuleBuilder<T, int> ruleBuilder) =>
        ruleBuilder.GreaterThanOrEqualTo(1);

    public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> ruleBuilder) =>
        ruleBuilder.InclusiveBetween(1, 100);
}
