namespace Beginor.SharpDb.Metadata;

internal sealed class SqliteMetadataProvider(
    IDbConnectionFactory connectionFactory,
    DatabaseOptions options
) : BaseMetadataProvider(connectionFactory, options) {

    protected override string GetTablesQuery() {
        // language=none
        return """
            select 'main' as table_schema,
                   objects.name as table_name,
                   case objects.type
                       when 'table' then 'BASE TABLE'
                       when 'view' then 'VIEW'
                   end as table_type,
                   null as table_description,
                   (
                       select group_concat(name, ', ')
                       from (
                           select table_info.name
                           from pragma_table_info(objects.name) table_info
                           where table_info.pk > 0
                           order by table_info.pk
                       )
                   ) as primary_key_columns,
                   (
                       select group_concat(
                           foreign_key."from" || ' -> main.' || foreign_key."table" || '(' || foreign_key."to" || ')',
                           '; '
                       )
                       from pragma_foreign_key_list(objects.name) foreign_key
                   ) as foreign_keys,
                   (
                       select group_concat(related_object, ', ')
                       from (
                           select distinct 'main.' || foreign_key."table" as related_object
                           from pragma_foreign_key_list(objects.name) foreign_key
                       )
                   ) as related_objects
            from sqlite_schema objects
            where objects.type in ('table', 'view')
              and objects.name not like 'sqlite_%'
              and (@schema is null or @schema = '' or @schema = 'main')
            order by objects.name
            """;
    }

    protected override string GetColumnsQuery() {
        return """
            select 'main' as table_schema,
                   @tableName as table_name,
                   table_info.name as column_name,
                   table_info.cid + 1 as ordinal_position,
                   table_info.type as data_type,
                   case
                       when table_info.pk > 0 then 'NO'
                       when table_info."notnull" = 0 then 'YES'
                       else 'NO'
                   end as is_nullable,
                   table_info.dflt_value as column_default,
                   null as column_description,
                   case when table_info.pk > 0 then 'YES' else 'NO' end as is_primary_key,
                   case when foreign_key.id is null then 'NO' else 'YES' end as is_foreign_key,
                   case when foreign_key."table" is null then null else 'main' end as referenced_table_schema,
                   foreign_key."table" as referenced_table_name,
                   foreign_key."to" as referenced_column_name
            from pragma_table_info(@tableName) table_info
            left join pragma_foreign_key_list(@tableName) foreign_key
              on foreign_key."from" = table_info.name
            where @schema is null or @schema = '' or @schema = 'main'
            order by table_info.cid
            """;
    }

}
