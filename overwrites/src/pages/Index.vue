<template>
  <Layout>
      <content-header
        :title="$static.metadata.siteName"
        :sub="$static.metadata.siteDescription"
        image="hero.png">
      </content-header>

      <div class="container mx-auto">
          <div class="flex flex-wrap my-4">

          <FeaturedCard v-if="$page.featured.totalCount>0" :records="$page.featured.edges"/>


          <CardItem v-for="edge in $page.entries.edges" :key="edge.node.id" :record="edge.node" />
        </div>
      </div>
  </Layout>
</template>

<page-query>
  query($page: Int) {
    featured: allBlog(limit: 4, filter: { featured: { eq: true } }, sortBy:"date") {
      totalCount
      edges {
        node {
          id
          title
          description
          image
          path
          timeToRead
          humanTime: date(format: "DD MMM YYYY")
          datetime: date
          category {
            id
            title
            path
          }
          author {
            id
            name
            image(width: 64, height: 64, fit: inside)
            path
          }
        }
      }
    }
    entries: allBlog(perPage: 24, page: $page, sortBy:"date") @paginate {
      totalCount
      pageInfo {
        totalPages
        currentPage
      }
      edges {
        node {
          id
          title
          description
          image
          path
          timeToRead
          featured
          humanTime: date(format: "DD MMM YYYY")
          datetime: date
          category {
            id
            title
            path
          }
          author {
            id
            name
            image(width: 64, height: 64, fit: inside)
            path
          }
        }
      }
    }
  }
</page-query>

<static-query>
query {
  metadata {
    siteName
    siteDescription
  }
}
</static-query>

<script>
import CardItem from "~/components/Content/CardItem.vue";
import FeaturedCard from "~/components/Content/FeaturedCard.vue";
import ContentHeader from "~/components/Partials/ContentHeader.vue";


export default {
  metaInfo: {
    title: "🏠"
  },
  components: {
    CardItem,
    FeaturedCard,
    ContentHeader
  }
};
</script>
