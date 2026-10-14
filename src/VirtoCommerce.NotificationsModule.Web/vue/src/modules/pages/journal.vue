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
      :state-key="stateKey"
      :active-item-id="activeChildParam"
      :show-all-columns="expanded"
      :global-filters="globalFilters"
      :empty-state="{ icon: 'lucide-history', title: $t('NOTIFICATIONS.PAGES.JOURNAL.EMPTY') }"
      @row-click="(e: { data: MessageEntity }) => open(e.data)"
      @pagination-click="pagination.goToPage"
      @filter="onFilter"
    >
      <template #search-header-actions>
        <VcSelect
          v-if="saved.length"
          v-model="savedId"
          class="ntf-saved"
          :options="saved"
          option-label="name"
          size="small"
          :placeholder="$t('NOTIFICATIONS.PAGES.JOURNAL.SAVED.PLACEHOLDER')"
        />
      </template>
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

    <VcPopup
      v-if="naming"
      :model-value="true"
      :title="$t('NOTIFICATIONS.PAGES.JOURNAL.SAVED.SAVE')"
      @update:model-value="naming = false"
    >
      <template #content>
        <form @submit.prevent="saveSearch">
          <VcInput
            v-model="savedName"
            :label="$t('NOTIFICATIONS.PAGES.JOURNAL.SAVED.NAME')"
            :hint="$t('NOTIFICATIONS.PAGES.JOURNAL.SAVED.HINT', { keyword: searchValue })"
            required
          />
        </form>
      </template>
      <template #footer>
        <div class="ntf-popup-footer">
          <VcButton
            variant="secondary"
            @click="naming = false"
          >
            {{ $t("NOTIFICATIONS.TOOLBAR.CANCEL") }}
          </VcButton>
          <VcButton
            :disabled="!savedName.trim()"
            @click="saveSearch"
          >
            {{ $t("NOTIFICATIONS.TOOLBAR.SAVE") }}
          </VcButton>
        </div>
      </template>
    </VcPopup>
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
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcButton, VcColumn, VcDataTable, VcInput, VcPopup, VcSelect, VcStatus } from "@vc-shell/framework/ui";
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
const statusVariant = (status?: string) => (status === "Sent" ? "success" : status === "Error" ? "danger" : "warning");
const statusLabel = (status?: string) => t(`NOTIFICATIONS.MESSAGE_STATUS.${(status ?? "Pending").toUpperCase()}`);

// the AngularJS journal filter panel: status, "search in body" and a period (today ... last 30 days, or a range)
const periods = ["today", "yesterday", "last24h", "last7d", "last30d"] as const;
function periodRange(period?: string): { startDate?: Date; endDate?: Date } {
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const daysAgo = (n: number) => new Date(today.getFullYear(), today.getMonth(), today.getDate() - n);
  switch (period) {
    case "today":
      return { startDate: today };
    case "yesterday":
      return { startDate: daysAgo(1), endDate: today };
    case "last24h":
      return { startDate: new Date(now.getTime() - 24 * 60 * 60 * 1000) };
    case "last7d":
      return { startDate: daysAgo(7) };
    case "last30d":
      return { startDate: daysAgo(30) };
    default:
      return {};
  }
}

const filters = ref<{ status?: string; startDate?: Date; endDate?: Date; searchInBody?: boolean }>({});
const globalFilters = computed(() => [
  {
    id: "searchInBody",
    label: t("NOTIFICATIONS.PAGES.JOURNAL.SEARCH_IN_BODY"),
    filter: { options: [{ value: "yes", label: t("NOTIFICATIONS.PAGES.JOURNAL.SEARCH_IN_BODY_YES") }] },
  },
  {
    id: "period",
    label: t("NOTIFICATIONS.PAGES.JOURNAL.PERIOD"),
    filter: { options: periods.map((p) => ({ value: p, label: t(`NOTIFICATIONS.PAGES.JOURNAL.PERIODS.${p.toUpperCase()}`) })) },
  },
  {
    id: "status",
    label: t("NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.STATUS"),
    filter: { options: statuses.map((s) => ({ value: s, label: statusLabel(s) })) },
  },
  {
    id: "dateRange",
    label: t("NOTIFICATIONS.PAGES.JOURNAL.COLUMNS.CREATED"),
    filter: { range: ["startDate", "endDate"] as [string, string] },
  },
]);

function onFilter(event: { filters: Record<string, unknown> }) {
  // an explicit Created range wins over the period
  const range =
    event.filters.startDate || event.filters.endDate
      ? {
          startDate: event.filters.startDate ? new Date(event.filters.startDate as string) : undefined,
          endDate: event.filters.endDate ? new Date(event.filters.endDate as string) : undefined,
        }
      : periodRange(event.filters.period as string);
  filters.value = {
    status: (event.filters.status as string) || undefined,
    searchInBody: event.filters.searchInBody === "yes" || undefined,
    ...range,
  };
  // from page 2 setPage(1) triggers the skip watcher, which loads
  if (pagination.skip) pagination.setPage(1);
  else load();
}

const { loading: listLoading, action: load } = useAsync(async () => {
  const result = await (await clients.notifications()).getNotificationJournal({
    objectIds: options.value?.objectId ? [options.value.objectId] : undefined,
    objectType: options.value?.objectType,
    notificationType: options.value?.notificationType,
    keyword: searchValue.value || undefined,
    ...filters.value,
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

// Saved searches: the AngularJS journal named keyword filters, kept per browser like its $localStorage list
type SavedSearch = { id: string; name: string; keyword: string };
const SAVED_KEY = "NOTIFICATIONS_JOURNAL_SAVED_SEARCHES";
function readSaved(): SavedSearch[] {
  try {
    return JSON.parse(localStorage.getItem(SAVED_KEY) ?? "[]");
  } catch {
    return [];
  }
}
const saved = ref<SavedSearch[]>(readSaved());
const savedId = ref<string>();
const naming = ref(false);
const savedName = ref("");
function writeSaved() {
  try {
    localStorage.setItem(SAVED_KEY, JSON.stringify(saved.value));
  } catch {
    // storage unavailable: the list just is not kept
  }
}
watch(savedId, (id) => {
  const found = saved.value.find((x) => x.id === id);
  if (found) searchValue.value = found.keyword;
});
// typing another search deselects the saved one (so picking it again re-applies it)
watch(searchValue, (value) => {
  if (savedId.value && saved.value.find((x) => x.id === savedId.value)?.keyword !== value) savedId.value = undefined;
});
function saveSearch() {
  const name = savedName.value.trim();
  if (!name || !searchValue.value) return;
  const existing = saved.value.find((x) => x.name === name);
  const entry = { id: existing?.id ?? String(Date.now()), name, keyword: searchValue.value };
  saved.value = [...saved.value.filter((x) => x.id !== entry.id), entry];
  writeSaved();
  savedId.value = entry.id;
  naming.value = false;
}
function deleteSaved() {
  saved.value = saved.value.filter((x) => x.id !== savedId.value);
  writeSaved();
  savedId.value = undefined;
}

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("NOTIFICATIONS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "saveSearch",
    title: t("NOTIFICATIONS.PAGES.JOURNAL.SAVED.SAVE"),
    icon: "lucide-bookmark-plus",
    disabled: computed(() => !searchValue.value),
    clickHandler: () => {
      savedName.value = saved.value.find((x) => x.id === savedId.value)?.name ?? "";
      naming.value = true;
    },
  },
  {
    id: "deleteSearch",
    title: t("NOTIFICATIONS.PAGES.JOURNAL.SAVED.DELETE"),
    icon: "lucide-bookmark-x",
    disabled: computed(() => !savedId.value),
    clickHandler: deleteSaved,
  },
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
onMounted(load);
</script>

<style lang="scss">
.ntf-saved {
  min-width: 180px;
}
</style>
