<template>
  <VcBlade
    :title="$t('NOTIFICATIONS.PAGES.JOURNAL.TITLE')"
    :subtitle="subtitle"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="60%"
  >
    <VcDataTable
      v-model:search-value="searchValue"
      v-model:sort-field="sortField"
      v-model:sort-order="sortOrder"
      v-model:selection="selection"
      selection-mode="multiple"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      search-bar
      :state-key="stateKey"
      :active-item-id="activeChildParam"
      :show-all-columns="expanded"
      :global-filters="globalFilters"
      :empty-state="{ icon: 'lucide-history', title: $t('NOTIFICATIONS.PAGES.JOURNAL.EMPTY') }"
      @row-click="(e: { data: MessageEntity }) => open(e.data)"
      @pagination-click="pagination.goToPage"
      @filter="onFilter"
    >
      <VcColumn
        id="notificationType"
        :title="$t('NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.NOTIFICATION')"
        body-class="ntf-list__name"
        sortable
        always-visible
      >
        <template #body="{ data }">{{ displayName(data.notificationType) }}</template>
      </VcColumn>
      <VcColumn
        id="recipient"
        :title="$t('NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.RECIPIENT')"
      >
        <template #body="{ data }">{{ data.to ?? data.number }}</template>
      </VcColumn>
      <VcColumn
        id="subject"
        :title="$t('NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.SUBJECT')"
      >
        <template #body="{ data }">{{ data.subject ?? data.message }}</template>
      </VcColumn>
      <VcColumn
        id="createdDate"
        :title="$t('NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.CREATED')"
        type="datetime"
        sortable
      />
      <VcColumn
        id="sendAttemptCount"
        :title="$t('NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.ATTEMPTS')"
        align="end"
      >
        <template #body="{ data }">{{ data.sendAttemptCount }}/{{ data.maxSendAttemptCount }}</template>
      </VcColumn>
      <VcColumn
        id="status"
        :title="$t('NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.STATUS')"
        always-visible
        sortable
      >
        <template #body="{ data }">
          <div class="tw-inline-flex">
            <VcStatus :variant="statusVariant(data.status)">{{ statusLabel(data.status) }}</VcStatus>
          </div>
        </template>
      </VcColumn>
    </VcDataTable>

  </VcBlade>
</template>

<script setup lang="ts">
import {
  useAsync,
  useBlade,
  useDataTablePagination,
  useDataTableSort,
  useFunctions,
  useLoading,
  useTableSearch,
  notification,
  type GlobalFilterConfig,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { displayName, useNotificationClients, type MessageEntity } from "../api";

defineBlade({
  url: "/notification-journal",
  name: "NotificationsJournal",
  permissions: ["notifications:read"],
});

const PAGE_SIZE = 20;

const { t } = useI18n({ useScope: "global" });
// from another blade: the messages of one object (order, store) or of one notification type
const { options, openBlade, activeChildParam, expanded } = useBlade<{ objectId?: string; objectType?: string; notificationType?: string; title?: string }>();
const { debounce } = useFunctions();
const clients = useNotificationClients();

const items = ref<MessageEntity[]>([]);
const selection = ref<MessageEntity[]>([]);
const totalCount = ref(0);
// only the global journal remembers its search/sort/page: a scoped one (an order's, a type's) starts clean
const scoped = !!(options.value?.objectId || options.value?.notificationType);
const stateKey = scoped ? undefined : "NOTIFICATIONS_JOURNAL";
const { searchValue } = useTableSearch({ stateKey });
const pagination = useDataTablePagination({ stateKey, pageSize: PAGE_SIZE, totalCount });
const { sortField, sortOrder, sortExpression } = useDataTableSort({
  stateKey,
  initialField: "createdDate",
  initialDirection: "DESC",
});

const subtitle = computed(
  () => options.value?.title ?? (options.value?.notificationType ? displayName(options.value.notificationType) : undefined),
);

const statuses = ["Sent", "Pending", "Error"];
function statusVariant(status?: string) {
  if (status === "Sent") return "success";
  if (status === "Error") return "danger";
  return "warning";
}
const statusLabel = (status?: string) => t(`NOTIFICATIONS.MESSAGE_STATUS.${(status ?? "Pending").toUpperCase()}`);

// The AngularJS journal filter panel: status, "search in body" and a period. The search bar's Created
// filter has the period presets (today, last 7 / 30 days) and a custom range; the type filter is only
// on the global journal (a type's own journal is already scoped to it).
const notificationTypes = ref<string[]>([]);
const filters = ref<{ notificationType?: string; status?: string; startDate?: Date; endDate?: Date; searchInBody?: boolean }>({});
// the search bar takes toggle fields, which vc-shell 2.13 does not yet type in GlobalFilterConfig
const globalFilters = computed(
  () =>
    [
      !options.value?.notificationType && {
        id: "notificationType",
        label: t("NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.NOTIFICATION"),
        filter: { options: notificationTypes.value.map((x) => ({ value: x, label: displayName(x) })) },
      },
      {
        id: "status",
        label: t("NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.STATUS"),
        filter: { options: statuses.map((s) => ({ value: s, label: statusLabel(s) })) },
      },
      {
        id: "createdDate",
        label: t("NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.CREATED"),
        filter: { range: ["startDate", "endDate"] },
      },
      { id: "searchInBody", label: t("NOTIFICATIONS.PAGES.JOURNAL.SEARCH_IN_BODY"), filter: { toggle: true } },
    ].filter(Boolean) as unknown as GlobalFilterConfig[],
);

// the search bar sends days (YYYY-MM-DD, both ends included); the API compares times, so the end is that day's last moment
const localDay = (ymd: unknown, endOfDay = false) => {
  if (typeof ymd !== "string" || !ymd) return undefined;
  const day = new Date(`${ymd}T00:00`);
  return endOfDay ? new Date(day.getFullYear(), day.getMonth(), day.getDate() + 1, 0, 0, 0, -1) : day;
};

function onFilter(event: { filters: Record<string, unknown> }) {
  filters.value = {
    notificationType: (event.filters.notificationType as string) || undefined,
    status: (event.filters.status as string) || undefined,
    searchInBody: event.filters.searchInBody === true || undefined,
    startDate: localDay(event.filters.startDate),
    endDate: localDay(event.filters.endDate, true),
  };
  // from page 2 setPage(1) triggers the skip watcher, which loads
  if (pagination.skip) pagination.setPage(1);
  else load();
}

async function loadNotificationTypes() {
  if (options.value?.notificationType) return;
  const result = await (await clients.notifications()).getNotifications({ take: 1000, responseGroup: "Default" });
  notificationTypes.value = (result.results ?? []).map((x) => x.type!).filter(Boolean).sort((a, b) => displayName(a).localeCompare(displayName(b)));
}

const { loading: listLoading, action: load } = useAsync(async () => {
  const result = await (await clients.notifications()).getNotificationJournal({
    objectIds: options.value?.objectId ? [options.value.objectId] : undefined,
    objectType: options.value?.objectType,
    keyword: searchValue.value || undefined,
    ...filters.value,
    notificationType: options.value?.notificationType ?? filters.value.notificationType,
    sort: sortExpression.value,
    skip: pagination.skip,
    take: PAGE_SIZE,
  });
  items.value = result.results ?? [];
  totalCount.value = result.totalCount ?? 0;
  selection.value = [];
});

function open(message: MessageEntity) {
  openBlade({ name: "NotificationMessageDetails", param: message.id });
}

const { loading: resendLoading, action: resend } = useAsync(async () => {
  await (await clients.notifications()).resendNotifications(selection.value.map((x) => x.id!));
  notification.success(t("NOTIFICATIONS.ALERTS.RESEND_SCHEDULED", { count: selection.value.length }));
  await load();
});

const loading = useLoading(listLoading, resendLoading);

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("NOTIFICATIONS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "resend",
    title: t("NOTIFICATIONS.PAGES.JOURNAL.RESEND"),
    icon: "lucide-repeat",
    permissions: ["notifications:update"],
    disabled: computed(() => !selection.value.length),
    clickHandler: () => resend(),
  },
]);

watch(searchValue, () => pagination.setPage(1));
watch([sortExpression, searchValue, () => pagination.skip], debounce(load, 300));
onMounted(() => {
  load();
  // the type filter's options; without them the filter just has none to offer
  loadNotificationTypes().catch(() => undefined);
});
</script>
