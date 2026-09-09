namespace Beginor.SharpDb.Metadata;

internal sealed class SqlServerMetadataProvider(
    IDbConnectionFactory connectionFactory,
    DatabaseOptions options
) : BaseMetadataProvider(connectionFactory, options) {

    protected override string GetTablesQuery() {
        // language=none
        return """
            select schemas.name as table_schema,
                   objects.name as table_name,
                   case objects.type
                       when 'U' then 'BASE TABLE'
                       when 'V' then 'VIEW'
                   end as table_type,
                   (
                       select cast(value as nvarchar(max))
                       from sys.extended_properties
                       where major_id = objects.object_id and minor_id = 0 and name = 'MS_Description'
                   ) as table_description,
                   (
                       select stuff(cast((
                           select ', ' + c.name
                           from sys.index_columns ic
                           join sys.columns c
                             on c.object_id = ic.object_id
                            and c.column_id = ic.column_id
                           where ic.object_id = objects.object_id
                             and ic.index_id = (
                                 select unique_index_id
                                 from sys.key_constraints
                                 where parent_object_id = objects.object_id and type = 'PK'
                             )
                           order by ic.key_ordinal
                           for xml path(''), type
                       ) as nvarchar(max)), 1, 2, '')
                   ) as primary_key_columns,
                   (
                       select replace(replace(replace(
                           stuff(cast((
                               select '; ' + c.name + ' -> ' +
                                      ref_schemas.name + '.' + ref_objects.name + '(' +
                                      ref_columns.name + ')'
                               from sys.foreign_key_columns fkc_cols
                               join sys.foreign_keys fk_constraints
                                 on fk_constraints.object_id = fkc_cols.constraint_object_id
                               join sys.columns c
                                 on c.object_id = fkc_cols.parent_object_id
                                and c.column_id = fkc_cols.parent_column_id
                               join sys.columns ref_columns
                                 on ref_columns.object_id = fkc_cols.referenced_object_id
                                and ref_columns.column_id = fkc_cols.referenced_column_id
                               join sys.objects ref_objects
                                 on ref_objects.object_id = fkc_cols.referenced_object_id
                               join sys.schemas ref_schemas
                                 on ref_schemas.schema_id = ref_objects.schema_id
                               where fkc_cols.parent_object_id = objects.object_id
                               order by fk_constraints.name, fkc_cols.parent_column_id
                               for xml path(''), type
                           ) as nvarchar(max)), 1, 2, ''),
                           '&lt;', '<'), '&gt;', '>'), '&amp;', '&')
                   ) as foreign_keys,
                   (
                       select stuff(cast((
                           select distinct ', ' + ref_schemas.name + '.' + ref_objects.name
                           from sys.foreign_key_columns fkc_cols
                           join sys.objects ref_objects
                             on ref_objects.object_id = fkc_cols.referenced_object_id
                           join sys.schemas ref_schemas
                             on ref_schemas.schema_id = ref_objects.schema_id
                           where fkc_cols.parent_object_id = objects.object_id
                           for xml path(''), type
                       ) as nvarchar(max)), 1, 2, '')
                   ) as related_objects
            from sys.objects objects
            join sys.schemas schemas
              on schemas.schema_id = objects.schema_id
            where objects.type in ('U', 'V')
              and objects.is_ms_shipped = 0
              and (@schema is null or @schema = '' or schemas.name = @schema)
            order by schemas.name, objects.name
            """;
    }

    protected override string GetColumnsQuery() {
        return """
            select schemas.name as table_schema,
                   objects.name as table_name,
                   columns.name as column_name,
                   columns.column_id as ordinal_position,
                   types.name as data_type,
                   case
                       when types.name in ('char', 'varchar') and columns.max_length <> -1 then columns.max_length
                       when types.name in ('nchar', 'nvarchar') and columns.max_length <> -1 then columns.max_length / 2
                   end as character_maximum_length,
                   case when columns.is_nullable = 1 then 'YES' else 'NO' end as is_nullable,
                   (
                       select substring(defaults.definition, 3, len(defaults.definition) - 4)
                       from sys.default_constraints defaults
                       where defaults.parent_object_id = columns.object_id
                         and defaults.parent_column_id = columns.column_id
                   ) as column_default,
                   (
                       select cast(value as nvarchar(max))
                       from sys.extended_properties
                       where major_id = columns.object_id and minor_id = columns.column_id and name = 'MS_Description'
                   ) as column_description,
                   case when primary_keys.column_id is null then 'NO' else 'YES' end as is_primary_key,
                   case when foreign_keys.parent_column_id is null then 'NO' else 'YES' end as is_foreign_key,
                   ref_schemas.name as referenced_table_schema,
                   ref_objects.name as referenced_table_name,
                   ref_columns.name as referenced_column_name
            from sys.columns columns
            join sys.objects objects
              on objects.object_id = columns.object_id
            join sys.schemas schemas
              on schemas.schema_id = objects.schema_id
            join sys.types types
              on types.user_type_id = columns.user_type_id
            left join (
                select ic.object_id, ic.column_id
                from sys.key_constraints constraints
                join sys.index_columns ic
                  on ic.object_id = constraints.parent_object_id
                 and ic.index_id = constraints.unique_index_id
                where constraints.type = 'PK'
            ) primary_keys
              on primary_keys.object_id = columns.object_id
             and primary_keys.column_id = columns.column_id
            left join sys.foreign_key_columns foreign_keys
              on foreign_keys.parent_object_id = columns.object_id
             and foreign_keys.parent_column_id = columns.column_id
            left join sys.objects ref_objects
              on ref_objects.object_id = foreign_keys.referenced_object_id
            left join sys.schemas ref_schemas
              on ref_schemas.schema_id = ref_objects.schema_id
            left join sys.columns ref_columns
              on ref_columns.object_id = foreign_keys.referenced_object_id
             and ref_columns.column_id = foreign_keys.referenced_column_id
            where objects.type in ('U', 'V')
              and objects.is_ms_shipped = 0
              and (@schema is null or @schema = '' or schemas.name = @schema)
              and objects.name = @tableName
            order by schemas.name, objects.name, columns.column_id
            """;
    }

}
